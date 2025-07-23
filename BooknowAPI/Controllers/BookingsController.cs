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
            var bookings = db.Bookings.Where(b => b.UserId == userId).ToList();
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

        // POST: api/bookings/create
        [HttpPost]
        [Route("create")]
        public IHttpActionResult Create(Booking booking)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var table = db.Tables.Find(booking.TableId);
            if (table == null)
                return BadRequest("Invalid table.");

            // Check if table is already booked in this timeslot (+100 min lock)
            if (table.Status == "Booked")
            {
                var recentBooking = db.Bookings
                    .Where(b => b.TableId == table.TableId)
                    .OrderByDescending(b => b.BookingDateTime)
                    .FirstOrDefault();

                if (recentBooking == null ||
                    booking.BookingDateTime >= (recentBooking.BookingDateTime ?? DateTime.MinValue).AddMinutes(100))
                {
                    // allow booking
                }
                else
                {
                    return BadRequest("This table is already booked during the selected time.");
                }
            }

            // Optional fields
            if (string.IsNullOrEmpty(booking.SpecialRequest))
                booking.SpecialRequest = null;

            if (booking.music_id == null)
                booking.CoinCategoryIdUsedForMusic = null;

            // Deduct coins if music selected
            if (booking.music_id != null && booking.CoinCategoryIdUsedForMusic != null)
            {
                var userCoin = db.CustomerCoins.FirstOrDefault(c =>
                    c.UserId == booking.UserId && c.CoinCategoryId == booking.CoinCategoryIdUsedForMusic);

                if (userCoin == null || userCoin.Balance < 10)
                    return BadRequest("Insufficient coins of selected category for music.");

                userCoin.Balance -= 10;
            }

            booking.Status = "Booked";
            db.Bookings.Add(booking);

            // Update table status
            table.Status = "Booked";

            db.SaveChanges();
            return Ok(booking);
        }

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
