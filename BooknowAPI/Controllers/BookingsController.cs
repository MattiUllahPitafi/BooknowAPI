using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;
using BooknowAPI.Models;

namespace RestWAdvBook.Controllers
{
    [RoutePrefix("api/bookings")]
    public class BookingsController : ApiController
    {
        private newRestdbEntities2 db = new newRestdbEntities2();

        // GET: api/bookings/getall
        [HttpGet]
        [Route("getall")]
        public IHttpActionResult GetAll()
        {
            var bookings = db.Bookings.Include(b => b.Table).Include(b => b.User).ToList();
            return Ok(bookings);
        }

        // GET: api/bookings/byuserid/{userId}
        [HttpGet]
        [Route("byuserid/{userId}")]
        public IHttpActionResult GetByUserId(int userId)
        {
            var bookings = db.Bookings
                .Where(b => b.UserId == userId)
                .Select(b => new
                {
                    RestaurantName = b.Restaurant != null ? b.Restaurant.Name : null,
                    b.BookingId,
                    b.BookingDateTime,
                    b.SpecialRequest,
                    b.Status,
                    b.TableId,
                    b.music_id
                })
                .ToList();

            return Ok(bookings);
        }


        // GET: api/bookings/bytableid/{tableId}
        [HttpGet]
        [Route("bytableid/{tableId}")]
        public IHttpActionResult GetByTableId(int tableId)
        {
            var bookings = db.Bookings.Where(b => b.TableId == tableId).ToList();
            return Ok(bookings);
        }

        // GET: api/bookings/byrestaurantid/{restaurantId}
        [HttpGet]
        [Route("byrestaurantid/{restaurantId}")]
        public IHttpActionResult GetByRestaurantId(int restaurantId)
        {
            var bookings = db.Bookings.Where(b => b.Table.RestaurantId == restaurantId).ToList();
            return Ok(bookings);
        }

        // GET: api/bookings/bydaterange?start=...&end=...
        [HttpGet]
        [Route("bydaterange")]
        public IHttpActionResult GetByDateRange(DateTime start, DateTime end)
        {
            var bookings = db.Bookings.Where(b => b.BookingDateTime >= start && b.BookingDateTime <= end).ToList();
            return Ok(bookings);
        }

        // GET: api/bookings/bystatus/{status}
        [HttpGet]
        [Route("bystatus/{status}")]
        public IHttpActionResult GetByStatus(string status)
        {
            var bookings = db.Bookings.Where(b => b.Status.ToLower() == status.ToLower()).ToList();
            return Ok(bookings);
        }
        [HttpPost]
        [Route("create")]
        public IHttpActionResult Create(Booking booking)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate Table
            var table = db.Tables.FirstOrDefault(t => t.TableId == booking.TableId);
            if (table == null)
                return BadRequest("Invalid table.");

            // Validate User
            var user = db.Users.FirstOrDefault(u => u.UserId == booking.UserId);
            if (user == null)
                return BadRequest("Invalid user.");

            // Optional Music Coin Handling
            if (booking.music_id != null && booking.CoinCategoryIdUsedForMusic != null)
            {
                var userCoin = db.CustomerCoins.FirstOrDefault(c =>
                    c.UserId == booking.UserId && c.CoinCategoryId == booking.CoinCategoryIdUsedForMusic);

                if (userCoin == null || userCoin.Balance < 10)
                    return BadRequest("Insufficient coins of selected category for music.");

                userCoin.Balance -= 10;
            }

            // === Check booking conflicts with a fixed 2-hour duration ===
            var requestedStart = booking.BookingDateTime ?? DateTime.MinValue;
            var requestedEnd = requestedStart.AddHours(2);

            // Pull only relevant bookings, then check overlaps in memory
            var bookingsForTable = db.Bookings
                .Where(b => b.TableId == table.TableId && b.Status == "Booked")
                .ToList();

            var overlappingBooking = bookingsForTable
                .FirstOrDefault(b =>
                    requestedStart < b.BookingDateTime.Value.AddHours(2) &&
                    requestedEnd > b.BookingDateTime.Value);

            if (overlappingBooking != null)
            {
                return BadRequest("This table is already booked during the selected time. " +
                                  $"Available after {overlappingBooking.BookingDateTime.Value.AddHours(2):hh:mm tt}.");
            }

            // Final booking prep
            booking.Status = "Booked";
            table.Status = "Booked"; // NOTE: consider per-timeslot availability instead of global "Booked"

            try
            {
                db.Bookings.Add(booking);
                db.SaveChanges(); // BookingId generated here
            }
            catch (Exception ex)
            {
                return BadRequest("Error saving booking: " + ex.Message +
                                  (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
            }

            // === Save to Jukebox if music is selected ===
            if (booking.music_id != null && booking.CoinCategoryIdUsedForMusic != null)
            {
                var jukebox = new Jukebox
                {
                    UserId = booking.UserId,
                    BookingId = booking.BookingId,
                    MusicId = booking.music_id.Value,
                    CoinCategoryId = booking.CoinCategoryIdUsedForMusic,
                    CoinsSpent = 10,
                    DedicationNote = booking.SpecialRequest,
                    RequestedAt = DateTime.Now
                };

                db.Jukeboxes.Add(jukebox);
                db.SaveChanges();
            }

            // === WAITER ASSIGNMENT ===
            var restaurantId = table.RestaurantId;

            var availableWaiters = db.Users
                .Where(u => u.Role == "Waiter" && u.Waiter.RestaurantId == restaurantId)
                .ToList();

            if (!availableWaiters.Any())
                return BadRequest("No waiters available for this restaurant.");

            var random = new Random();
            var selectedWaiter = availableWaiters[random.Next(availableWaiters.Count)];

            var waiterAssignment = new WaiterAssignment
            {
                BookingId = booking.BookingId,
                WaiterUserId = selectedWaiter.UserId,
                AssignedAt = DateTime.Now
            };

            db.WaiterAssignments.Add(waiterAssignment);
            db.SaveChanges();

            return Ok(new
            {
                booking.BookingId,
                Message = "Booking created, music saved, and waiter assigned successfully."
            });
        }

        //[HttpPost]
        //[Route("create")]
        //public IHttpActionResult Create(Booking booking)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    // Validate Table
        //    var table = db.Tables.FirstOrDefault(t => t.TableId == booking.TableId);
        //    if (table == null)
        //        return BadRequest("Invalid table.");

        //    // Validate User
        //    var user = db.Users.FirstOrDefault(u => u.UserId == booking.UserId);
        //    if (user == null)
        //        return BadRequest("Invalid user.");

        //    // Optional Music Coin Handling
        //    if (booking.music_id != null && booking.CoinCategoryIdUsedForMusic != null)
        //    {
        //        var userCoin = db.CustomerCoins.FirstOrDefault(c =>
        //            c.UserId == booking.UserId && c.CoinCategoryId == booking.CoinCategoryIdUsedForMusic);

        //        if (userCoin == null || userCoin.Balance < 10)
        //            return BadRequest("Insufficient coins of selected category for music.");

        //        userCoin.Balance -= 10;
        //    }

        //    // Check recent booking conflict
        //    if (table.Status == "Booked")
        //    {
        //        var recentBooking = db.Bookings
        //            .Where(b => b.TableId == table.TableId)
        //            .OrderByDescending(b => b.BookingDateTime)
        //            .FirstOrDefault();

        //        if (recentBooking != null &&
        //            booking.BookingDateTime < (recentBooking.BookingDateTime ?? DateTime.MinValue).AddMinutes(100))
        //        {
        //            return BadRequest("This table is already booked during the selected time.");
        //        }
        //    }

        //    // Final booking prep
        //    booking.Status = "Booked";
        //    table.Status = "Booked";

        //    try
        //    {
        //        db.Bookings.Add(booking);
        //        db.SaveChanges(); // BookingId generated here
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest("Error saving booking: " + ex.Message +
        //                          (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
        //    }

        //    // === Save to Jukebox if music is selected ===
        //    if (booking.music_id != null && booking.CoinCategoryIdUsedForMusic != null)
        //    {
        //        var jukebox = new Jukebox
        //        {
        //            UserId = booking.UserId,
        //            BookingId = booking.BookingId,
        //            MusicId = booking.music_id.Value,
        //            CoinCategoryId = booking.CoinCategoryIdUsedForMusic,
        //            CoinsSpent = 10,
        //            DedicationNote = booking.SpecialRequest,
        //            RequestedAt = DateTime.Now
        //        };

        //        db.Jukeboxes.Add(jukebox);
        //        db.SaveChanges();
        //    }

        //    // === WAITER ASSIGNMENT ===
        //    var restaurantId = table.RestaurantId;

        //    var availableWaiters = db.Users
        //        .Where(u => u.Role == "Waiter" && u.Waiter.RestaurantId == restaurantId)
        //        .ToList();

        //    if (!availableWaiters.Any())
        //        return BadRequest("No waiters available for this restaurant.");

        //    var random = new Random();
        //    var selectedWaiter = availableWaiters[random.Next(availableWaiters.Count)];

        //    var waiterAssignment = new WaiterAssignment
        //    {
        //        BookingId = booking.BookingId,
        //        WaiterUserId = selectedWaiter.UserId,
        //        AssignedAt = DateTime.Now
        //    };

        //    db.WaiterAssignments.Add(waiterAssignment);
        //    db.SaveChanges();

        //    return Ok(new
        //    {
        //        booking.BookingId,
        //        Message = "Booking created, music saved, and waiter assigned successfully."
        //    });
        //}


        // PUT: api/bookings/cancel/{id}
        [HttpPut]
        [Route("cancel/{id}")]
        public IHttpActionResult CancelBooking(int id)
        {
            var booking = db.Bookings.Find(id);
            if (booking == null)
                return NotFound();

            var timeLeft = (booking.BookingDateTime ?? DateTime.MinValue) - DateTime.Now;
            if (timeLeft.TotalMinutes < 120)
                return BadRequest("Cancellation not allowed within 2 hours of booking time.");

            booking.Status = "Cancelled";
            db.SaveChanges();

            return Ok("Booking cancelled.");
        }

        // PUT: api/bookings/releasetable/{tableId}
        [HttpPut]
        [Route("releasetable/{tableId}")]
        public IHttpActionResult ReleaseTable(int tableId)
        {
            var table = db.Tables.Find(tableId);
            if (table == null)
                return NotFound();

            var latestBooking = db.Bookings
                .Where(b => b.TableId == tableId && b.Status == "Booked")
                .OrderByDescending(b => b.BookingDateTime)
                .FirstOrDefault();

            if (latestBooking != null &&
                DateTime.Now >= (latestBooking.BookingDateTime ?? DateTime.MinValue).AddMinutes(100))
            {
                table.Status = "Available";
                db.SaveChanges();
                return Ok("Table released.");
            }

            return BadRequest("Table is either not booked or still within lock period.");
        }
    }
}
