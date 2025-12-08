using BooknowAPI.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace RestWAdvBook.Controllers
{
    [RoutePrefix("api/bookings")]

    public class BookingsController : ApiController
    {
        private newRestdbEntities7 db = new newRestdbEntities7();

        // GET: api/bookings/getall
        [HttpGet]
        [Route("getall")]
        public IHttpActionResult GetAll()
        {
            var bookings = db.Bookings.Include(b => b.Table).Include(b => b.User).ToList();
            return Ok(bookings);
        }

        // GET: api/bookings/byuserid/{userId}
        //[HttpGet]
        //[Route("byuserid/{userId}")]
        //public IHttpActionResult GetByUserId(int userId)
        //{
        //    var bookings = db.Bookings
        //        .Where(b => b.UserId == userId)
        //        .Select(b => new
        //        {
        //            RestaurantName = b.Restaurant != null ? b.Restaurant.Name : null,
        //            b.BookingId,
        //            b.BookingDateTime,
        //            b.SpecialRequest,
        //            b.Status,
        //            b.TableId,
        //            b.music_id
        //        })
        //        .ToList();

        //    return Ok(bookings);
        //}
        //[HttpGet]
        //[Route("byuserid/{userId}")]
        //public IHttpActionResult GetByUserId(int userId)
        //{
        //    var bookings = db.Bookings
        //        .Where(b => b.UserId == userId)
        //        .Select(b => new
        //        {
        //            bookingId = b.BookingId,
        //            restaurantName = b.Restaurant != null ? b.Restaurant.Name : "",    // never null
        //            bookingDateTime = b.BookingDateTime.ToString(),                     // never null
        //            specialRequest = b.SpecialRequest ?? "",                             // default empty
        //            status = b.Status ?? "Pending",                                      // default
        //            tableId = b.TableId,
        //            music_id = b.music_id ?? 0                                          // default
        //        })
        //        .ToList();

        //    return Ok(bookings);
        //}

        [HttpGet]
        [Route("byuserid/{userId}")]
        public IHttpActionResult GetByUserId(int userId)
        {
            var bookings = db.Bookings
                .Where(b => b.UserId == userId && b.TableId != null && b.MasterBookingId != null)  // Exclude rows with null TableId and MasterBookingId
                .Select(b => new
                {
                    bookingId = b.BookingId,
                    masterBookingId = b.MasterBookingId,  // Include MasterBookingId to identify groups
                    restaurantName = b.Restaurant != null ? b.Restaurant.Name : "",  // never null
                    bookingDateTime = b.BookingDateTime.ToString(),  // never null
                    specialRequest = b.SpecialRequest ?? "",  // default empty
                    status = b.Status ?? "Pending",  // default
                    tableId = b.TableId,
                    music_id = b.music_id ?? 0  // default
                })
                .ToList();

            // Step 1: Handle Multi-table Bookings Based on MasterBookingId
            // Group bookings that share the same MasterBookingId, which indicates multi-table bookings.
            var groupedBookings = bookings
                .GroupBy(b => b.masterBookingId)  // Group by MasterBookingId to identify multi-table bookings
                .Select(g => new
                {
                    masterBookingId = g.Key,
                    bookings = g.ToList()  // Collect all bookings that belong to the same master booking
                })
                .ToList();

            // Step 2: Flatten grouped bookings back into a list while preserving the structure for the frontend.
            var result = new List<object>();
            foreach (var group in groupedBookings)
            {
                foreach (var booking in group.bookings)
                {
                    var resultBooking = new
                    {
                        bookingId = booking.bookingId,
                        restaurantName = booking.restaurantName,
                        bookingDateTime = booking.bookingDateTime,
                        specialRequest = group.bookings.First().specialRequest,  // Use the same special request for all tables in the group
                        status = booking.status,
                        tableId = booking.tableId,
                        music_id = booking.music_id
                    };
                    result.Add(resultBooking);
                }
            }

            return Ok(result);  // Return the flat list of bookings
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


        //[HttpPost]
        //[Route("create")]
        //public IHttpActionResult Create(Booking booking)
        //{
        //    // Constant for the initial safe booking duration (1.5 hours)
        //    const int DefaultDiningMinutes = 90;

        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    // === Validate Table ===
        //    var table = db.Tables.FirstOrDefault(t => t.TableId == booking.TableId);
        //    if (table == null)
        //        return BadRequest("Invalid table.");

        //    // === Validate User ===
        //    var user = db.Users.FirstOrDefault(u => u.UserId == booking.UserId);
        //    if (user == null)
        //        return BadRequest("Invalid user.");
        //    if (booking.BookingDateTime.HasValue)
        //    {
        //        // If the client sends local time (3:46 PM) and you want to save as UTC
        //        // First specify it as Local, then convert to UTC
        //        var localTime = DateTime.SpecifyKind(booking.BookingDateTime.Value, DateTimeKind.Local);
        //        booking.BookingDateTime = localTime.ToUniversalTime();

        //        // Now when you read it back, convert from UTC to Local
        //        // var displayTime = booking.BookingDateTime.Value.ToLocalTime();
        //    }

        //    // ... (Existing Music, Coin, and Dedication Note Handling) ...

        //    // === Optional Music Handling (requires coins) ===
        //    if (booking.music_id.HasValue && booking.CoinCategoryIdUsedForMusic.HasValue)
        //    {
        //        var userCoin = db.CustomerCoins.FirstOrDefault(c =>
        //             c.UserId == booking.UserId &&
        //             c.CoinCategoryId == booking.CoinCategoryIdUsedForMusic);

        //        if (userCoin == null || userCoin.Balance < 10)
        //            return BadRequest("Insufficient coins of selected category for music.");

        //        // Deduct 10 coins
        //        userCoin.Balance -= 10;
        //    }

        //    // === Check booking conflicts (Using Dynamic Logic) ===
        //    var requestedStart = booking.BookingDateTime ?? DateTime.MinValue;
        //    // Define the NEW booking window using the default duration
        //    var requestedEnd = requestedStart.AddMinutes(DefaultDiningMinutes);

        //    var bookingsForTable = db.Bookings
        //        .Where(b => b.TableId == table.TableId && b.Status == "Booked")
        //        .ToList();

        //    var overlappingBooking = bookingsForTable.FirstOrDefault(b =>
        //        // Check against EXISTING dynamic duration (b.MaxEstimatedMinutes)
        //        requestedStart < b.BookingDateTime.Value.AddMinutes(b.MaxEstimatedMinutes) &&
        //        requestedEnd > b.BookingDateTime.Value);

        //    if (overlappingBooking != null)
        //    {
        //        // Show available time using EXISTING dynamic duration
        //        var availableTime = overlappingBooking.BookingDateTime.Value.AddMinutes(overlappingBooking.MaxEstimatedMinutes);
        //        return BadRequest("This table is already booked during the selected time. " +
        //                            $"Available after {availableTime:hh:mm tt}.");
        //    }

        //    // === Final booking setup ===
        //    booking.Status = "Booked";
        //    table.Status = "Booked";
        //    // *** Set the initial default duration on the NEW booking ***
        //    booking.MaxEstimatedMinutes = DefaultDiningMinutes;

        //    try
        //    {
        //        db.Bookings.Add(booking);
        //        db.SaveChanges(); // ✅ BookingId generated here
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest("Error saving booking: " + ex.Message +
        //                            (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
        //    }

        //    // ... (Rest of the method: Jukebox and Waiter Assignment logic) ...
        //    if (booking.music_id.HasValue && booking.CoinCategoryIdUsedForMusic.HasValue)
        //    {
        //        var jukebox = new Jukebox
        //        {
        //            UserId = booking.UserId,
        //            BookingId = booking.BookingId,
        //            MusicId = booking.music_id.Value,
        //            CoinCategoryId = booking.CoinCategoryIdUsedForMusic,
        //            CoinsSpent = 10,
        //            DedicationNote = booking.DedicationNote,
        //            RequestedAt = DateTime.Now
        //        };

        //        db.Jukeboxes.Add(jukebox);
        //        db.SaveChanges();
        //    }

        //    // === Waiter Assignment ===
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
        //        Message = "Booking created successfully with optional music and waiter assigned."
        //    });
        //}ya kam kr rha h oper wla
        [HttpPost]
        [Route("create")]
        public IHttpActionResult Create(Booking booking)
        {
            System.Diagnostics.Debug.WriteLine(
    "Incoming BookingDateTime = " + booking.BookingDateTime
);

            const int DefaultDiningMinutes = 90;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // === Validate Table ===
            var table = db.Tables.FirstOrDefault(t => t.TableId == booking.TableId);
            if (table == null)
                return BadRequest("Invalid table.");
            booking.RestaurantId = table.RestaurantId;


            // === Validate User ===
            var user = db.Users.FirstOrDefault(u => u.UserId == booking.UserId);
            if (user == null)
                return BadRequest("Invalid user.");

            // === 🔒 FORCE UTC CONVERSION (SAFE) ===
            //if (booking.BookingDateTime.HasValue)
            //{
            //    var dt = booking.BookingDateTime.Value;

            //    if (dt.Kind == DateTimeKind.Unspecified)
            //    {
            //        booking.BookingDateTime =
            //            DateTime.SpecifyKind(dt, DateTimeKind.Local)
            //                    .ToUniversalTime();
            //    }
            //    else if (dt.Kind == DateTimeKind.Utc)
            //    {
            //        booking.BookingDateTime = dt;
            //    }
            //    else if (dt.Kind == DateTimeKind.Local)
            //    {
            //        booking.BookingDateTime = dt.ToUniversalTime();
            //    }
            //}

            // === Optional Music Handling ===
            if (booking.music_id.HasValue && booking.CoinCategoryIdUsedForMusic.HasValue)
            {
                var userCoin = db.CustomerCoins.FirstOrDefault(c =>
                    c.UserId == booking.UserId &&
                    c.CoinCategoryId == booking.CoinCategoryIdUsedForMusic);

                if (userCoin == null || userCoin.Balance < 10)
                    return BadRequest("Insufficient coins of selected category for music.");

                userCoin.Balance -= 10;
            }

            // === Booking Conflict Check (UTC vs UTC) ===
            var requestedStart = booking.BookingDateTime.Value;
            var requestedEnd = requestedStart.AddMinutes(DefaultDiningMinutes);

            var bookingsForTable = db.Bookings
                .Where(b => b.TableId == table.TableId && b.Status == "Booked")
                .ToList();

            var overlappingBooking = bookingsForTable.FirstOrDefault(b =>
                requestedStart < b.BookingDateTime.Value.AddMinutes(b.MaxEstimatedMinutes) &&
                requestedEnd > b.BookingDateTime.Value);

            if (overlappingBooking != null)
            {
                var availableTimeLocal =
                    overlappingBooking.BookingDateTime.Value
                        .AddMinutes(overlappingBooking.MaxEstimatedMinutes)
                        .ToLocalTime();

                return BadRequest(
                    $"This table is already booked. Available after {availableTimeLocal:hh:mm tt}."
                );
            }

            // === Final Booking Setup ===
            booking.Status = "Booked";
            booking.MaxEstimatedMinutes = DefaultDiningMinutes;
            table.Status = "Booked";

            try
            {
                db.Bookings.Add(booking);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return BadRequest(
                    "Error saving booking: " + ex.Message +
                    (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : "")
                );
            }

            // === Jukebox Entry ===
            if (booking.music_id.HasValue && booking.CoinCategoryIdUsedForMusic.HasValue)
            {
                db.Jukeboxes.Add(new Jukebox
                {
                    UserId = booking.UserId,
                    BookingId = booking.BookingId,
                    MusicId = booking.music_id.Value,
                    CoinCategoryId = booking.CoinCategoryIdUsedForMusic,
                    CoinsSpent = 10,
                    DedicationNote = booking.DedicationNote,
                    RequestedAt = DateTime.UtcNow
                });

                db.SaveChanges();
            }

            // === Waiter Assignment ===
            var availableWaiters = db.Users
                .Where(u => u.Role == "Waiter" && u.Waiter.RestaurantId == table.RestaurantId)
                .ToList();

            if (!availableWaiters.Any())
                return BadRequest("No waiters available for this restaurant.");

            var selectedWaiter = availableWaiters[new Random().Next(availableWaiters.Count)];

            db.WaiterAssignments.Add(new WaiterAssignment
            {
                BookingId = booking.BookingId,
                WaiterUserId = selectedWaiter.UserId,
                AssignedAt = DateTime.UtcNow
            });

            db.SaveChanges();

            return Ok(new
            {
                booking.BookingId,
                Message = "Booking created successfully."
            });
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


[HttpPost]
    [Route("create-multiple")]
    public IHttpActionResult CreateMultiple([FromBody] MultipleTablesBookingRequest request)
    {
        const int DefaultDiningMinutes = 90;

        if (request == null || request.TableIds == null || !request.TableIds.Any())
            return BadRequest("No tables selected.");

        // ✅ Validate user
        var user = db.Users.FirstOrDefault(u => u.UserId == request.UserId);
        if (user == null)
            return BadRequest("Invalid user.");

        // ✅ FIX 1: get first table ID in memory (NOT inside EF)
        int firstTableId = request.TableIds.First();
        var firstTable = db.Tables.FirstOrDefault(t => t.TableId == firstTableId);
        if (firstTable == null)
            return BadRequest("Invalid table in selection.");

        int restaurantId = firstTable.RestaurantId.Value;
        DateTime bookingTime = request.BookingDateTime;

        // =====================
        // MASTER BOOKING
        // =====================
        var masterBooking = new Booking
        {
            UserId = request.UserId,
            RestaurantId = restaurantId,
            BookingDateTime = bookingTime,
            Status = "Booked",
            MaxEstimatedMinutes = DefaultDiningMinutes,
            SpecialRequest = request.SpecialRequest
        };

        db.Bookings.Add(masterBooking);
        db.SaveChanges(); // generates BookingId

        // =====================
        // CHILD BOOKINGS
        // =====================
        foreach (var tableId in request.TableIds)
        {
            var table = db.Tables.FirstOrDefault(t => t.TableId == tableId);
            if (table == null)
                return BadRequest($"Invalid table ID: {tableId}");

            DateTime requestedStart = bookingTime;
            DateTime requestedEnd = bookingTime.AddMinutes(DefaultDiningMinutes);

            // ✅ FIX 2: DbFunctions instead of AddMinutes
            bool conflict = db.Bookings.Any(b =>
                b.TableId == tableId &&
                b.Status == "Booked" &&
                requestedStart < DbFunctions.AddMinutes(b.BookingDateTime, b.MaxEstimatedMinutes) &&
                requestedEnd > b.BookingDateTime
            );

            if (conflict)
                return BadRequest($"Table {tableId} is already booked.");

            var childBooking = new Booking
            {
                MasterBookingId = masterBooking.BookingId.ToString(),
                UserId = request.UserId,
                RestaurantId = table.RestaurantId,
                TableId = tableId,
                BookingDateTime = bookingTime,
                Status = "Booked",
                MaxEstimatedMinutes = DefaultDiningMinutes,
                SpecialRequest = request.SpecialRequest // ✅ ADD THIS

            };

            db.Bookings.Add(childBooking);
            table.Status = "Booked";
        }

        db.SaveChanges();

            var firstChildBookingId = db.Bookings
         .Where(b => b.MasterBookingId == masterBooking.BookingId.ToString())
         .Select(b => b.BookingId)
         .FirstOrDefault();

            return Ok(new
            {
                BookingId = firstChildBookingId,   // 👈 frontend-friendly
                MasterBookingId = masterBooking.BookingId,
                Message = "Multiple tables booked successfully."
            });

        }


        //[HttpPost]
        //[Route("create-multiple")]
        //public IHttpActionResult CreateMultiple(MultipleTablesBookingRequest request)
        //{
        //    const int DefaultDiningMinutes = 90;

        //    if (request == null || request.TableIds == null || !request.TableIds.Any())
        //        return BadRequest("No tables selected.");

        //    if (!request.BookingDateTime.HasValue)
        //        return BadRequest("BookingDateTime is required.");

        //    // === Validate User ===
        //    var user = db.Users.FirstOrDefault(u => u.UserId == request.UserId);
        //    if (user == null)
        //        return BadRequest("Invalid user.");

        //    // === MASTER BOOKING (NO TABLE) ===
        //    var masterBooking = new Booking
        //    {
        //        UserId = request.UserId,
        //        BookingDateTime = request.BookingDateTime,
        //        Status = "Booked",
        //        MaxEstimatedMinutes = DefaultDiningMinutes,
        //        SpecialRequest = request.SpecialRequest
        //    };

        //    db.Bookings.Add(masterBooking);
        //    db.SaveChanges(); // generates BookingId

        //    // === CHILD BOOKINGS (ONE PER TABLE) ===
        //    foreach (var tableId in request.TableIds)
        //    {
        //        var table = db.Tables.FirstOrDefault(t => t.TableId == tableId);
        //        if (table == null)
        //            return BadRequest($"Invalid table ID: {tableId}");

        //        var requestedStart = request.BookingDateTime.Value;
        //        var requestedEnd = requestedStart.AddMinutes(DefaultDiningMinutes);

        //        var conflict = db.Bookings.Any(b =>
        //            b.TableId == tableId &&
        //            b.Status == "Booked" &&
        //            requestedStart < b.BookingDateTime.Value.AddMinutes(b.MaxEstimatedMinutes) &&
        //            requestedEnd > b.BookingDateTime.Value
        //        );

        //        if (conflict)
        //            return BadRequest($"Table {tableId} is already booked.");

        //        var childBooking = new Booking
        //        {
        //            MasterBookingId = masterBooking.BookingId.ToString(),
        //            UserId = request.UserId,
        //            TableId = tableId,
        //            BookingDateTime = request.BookingDateTime,
        //            Status = "Booked",
        //            MaxEstimatedMinutes = DefaultDiningMinutes
        //        };

        //        db.Bookings.Add(childBooking);
        //        table.Status = "Booked";
        //    }

        //    db.SaveChanges();

        //    return Ok(new
        //    {
        //        MasterBookingId = masterBooking.BookingId,
        //        Message = "Multiple tables booked successfully."
        //    });
        //}


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

// Add this class inside your BookingsController class or above it
public class MultipleTablesBookingRequest
{
    public int UserId { get; set; }
    public List<int> TableIds { get; set; }
    public DateTime BookingDateTime { get; set; }
    public string SpecialRequest { get; set; }
    public int? MusicId { get; set; }
    public int? CoinCategoryIdUsedForMusic { get; set; }
    public string DedicationNote { get; set; }
}
