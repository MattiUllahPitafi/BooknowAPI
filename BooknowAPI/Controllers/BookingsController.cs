using BooknowAPI.Models;
using System;
using System.Data.Entity;
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
        [HttpGet]
        [Route("byuserid/{userId}")]
        public IHttpActionResult GetByUserId(int userId)
        {
            var bookings = db.Bookings
                .Where(b => b.UserId == userId)
                .Select(b => new
                {
                    bookingId = b.BookingId,
                    restaurantName = b.Restaurant != null ? b.Restaurant.Name : "",    // never null
                    bookingDateTime = b.BookingDateTime.ToString(),                     // never null
                    specialRequest = b.SpecialRequest ?? "",                             // default empty
                    status = b.Status ?? "Pending",                                      // default
                    tableId = b.TableId,
                    music_id = b.music_id ?? 0                                          // default
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

        //    //// Optional Music Coin Handling
        //    //if (booking.music_id != null && booking.CoinCategoryIdUsedForMusic != null)
        //    //{
        //    //    var userCoin = db.CustomerCoins.FirstOrDefault(c =>
        //    //        c.UserId == booking.UserId && c.CoinCategoryId == booking.CoinCategoryIdUsedForMusic);

        //    //    if (userCoin == null || userCoin.Balance < 10)
        //    //        return BadRequest("Insufficient coins of selected category for music.");

        //    //    userCoin.Balance -= 10;
        //    //}// === Optional Music Handling ===
        //    //bool hasMusic = booking.music_id.HasValue; // Check if user selected music
        //    //bool hasCoinCategory = booking.CoinCategoryIdUsedForMusic.HasValue;

        //    // === Optional Music Handling (requires coins) ===
        //    if (booking.music_id.HasValue && booking.CoinCategoryIdUsedForMusic.HasValue)
        //    {
        //        // Validate user's coin balance
        //        var userCoin = db.CustomerCoins.FirstOrDefault(c =>
        //            c.UserId == booking.UserId &&
        //            c.CoinCategoryId == booking.CoinCategoryIdUsedForMusic);

        //        if (userCoin == null || userCoin.Balance < 10)
        //            return BadRequest("Insufficient coins of selected category for music.");

        //        // Deduct 10 coins
        //        userCoin.Balance -= 10;
        //    }


        //    // === Check booking conflicts with a fixed 2-hour duration ===
        //    var requestedStart = booking.BookingDateTime ?? DateTime.MinValue;
        //    var requestedEnd = requestedStart.AddHours(2);

        //    // Pull only relevant bookings, then check overlaps in memory
        //    var bookingsForTable = db.Bookings
        //        .Where(b => b.TableId == table.TableId && b.Status == "Booked")
        //        .ToList();

        //    var overlappingBooking = bookingsForTable
        //        .FirstOrDefault(b =>
        //            requestedStart < b.BookingDateTime.Value.AddHours(2) &&
        //            requestedEnd > b.BookingDateTime.Value);

        //    if (overlappingBooking != null)
        //    {
        //        return BadRequest("This table is already booked during the selected time. " +
        //                          $"Available after {overlappingBooking.BookingDateTime.Value.AddHours(2):hh:mm tt}.");
        //    }

        //    // Final booking prep
        //    booking.Status = "Booked";
        //    table.Status = "Booked"; // NOTE: consider per-timeslot availability instead of global "Booked"

        //    //try
        //    //{
        //    //    db.Bookings.Add(booking);
        //    //    db.SaveChanges(); // BookingId generated here
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    return BadRequest("Error saving booking: " + ex.Message +
        //    //                      (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
        //    //}
        //    try
        //    {
        //        // Explicitly assign music & dedication if provided (fix for casing mismatch)
        //        if (booking.music_id == null && HttpContext.Current.Request["musicId"] != null)
        //            booking.music_id = int.Parse(HttpContext.Current.Request["musicId"]);

        //        if (string.IsNullOrEmpty(booking.SpecialRequest) && HttpContext.Current.Request["specialRequest"] != null)
        //            booking.SpecialRequest = HttpContext.Current.Request["specialRequest"];

        //        // Default booking status
        //        booking.Status = booking.Status ?? "Booked";

        //        db.Bookings.Add(booking);
        //        db.SaveChanges(); // BookingId generated here ✅ now includes music_id
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest("Error saving booking: " + ex.Message +
        //                          (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
        //    }


        //    // === Save to Jukebox if music is selected ===
        //    //if (booking.music_id != null && booking.CoinCategoryIdUsedForMusic != null)
        //    //{
        //    //    var jukebox = new Jukebox
        //    //    {
        //    //        UserId = booking.UserId,
        //    //        BookingId = booking.BookingId,
        //    //        MusicId = booking.music_id.Value,
        //    //        CoinCategoryId = booking.CoinCategoryIdUsedForMusic,
        //    //        CoinsSpent = 10,
        //    //        DedicationNote = booking.DedicationNote,
        //    //        RequestedAt = DateTime.Now
        //    //    };

        //    //    db.Jukeboxes.Add(jukebox);
        //    //    db.SaveChanges();
        //    //}
        //    // === Save to Jukebox only if music is selected ===
        //    // === Save to Jukebox if music is selected ===
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

        //[HttpPost]yahan tak sb thek h estimate time chkr kr rha hn
        //[Route("create")]
        //public IHttpActionResult Create(Booking booking)
        //{
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

        //    // === Try auto-detect alternate JSON keys (musicId, dedicationNote etc.) ===
        //    var httpRequest = HttpContext.Current?.Request;
        //    if (booking.music_id == null && int.TryParse(httpRequest?["musicId"], out int parsedMusic))
        //        booking.music_id = parsedMusic;

        //    if (booking.CoinCategoryIdUsedForMusic == null && int.TryParse(httpRequest?["coinCategoryIdUsedForMusic"], out int parsedCoin))
        //        booking.CoinCategoryIdUsedForMusic = parsedCoin;

        //    if (string.IsNullOrEmpty(booking.DedicationNote) && !string.IsNullOrEmpty(httpRequest?["dedicationNote"]))
        //        booking.DedicationNote = httpRequest["dedicationNote"];

        //    // === Optional Music Handling (requires coins) ===
        //    if (booking.music_id.HasValue && booking.CoinCategoryIdUsedForMusic.HasValue)
        //    {
        //        var userCoin = db.CustomerCoins.FirstOrDefault(c =>
        //            c.UserId == booking.UserId &&
        //            c.CoinCategoryId == booking.CoinCategoryIdUsedForMusic);

        //        if (userCoin == null || userCoin.Balance < 10)
        //            return BadRequest("Insufficient coins of selected category for music.");

        //        // Deduct 10 coins
        //        userCoin.Balance -= 10;
        //    }

        //    // === Check booking conflicts ===
        //    var requestedStart = booking.BookingDateTime ?? DateTime.MinValue;
        //    var requestedEnd = requestedStart.AddHours(2);

        //    var bookingsForTable = db.Bookings
        //        .Where(b => b.TableId == table.TableId && b.Status == "Booked")
        //        .ToList();

        //    var overlappingBooking = bookingsForTable.FirstOrDefault(b =>
        //        requestedStart < b.BookingDateTime.Value.AddHours(2) &&
        //        requestedEnd > b.BookingDateTime.Value);

        //    if (overlappingBooking != null)
        //    {
        //        return BadRequest("This table is already booked during the selected time. " +
        //                          $"Available after {overlappingBooking.BookingDateTime.Value.AddHours(2):hh:mm tt}.");
        //    }

        //    // === Final booking setup ===
        //    booking.Status = "Booked";
        //    table.Status = "Booked";

        //    try
        //    {
        //        db.Bookings.Add(booking);
        //        db.SaveChanges(); // ✅ BookingId generated here with music_id included
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest("Error saving booking: " + ex.Message +
        //                          (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
        //    }

        //    // === Save to Jukebox if music is selected ===
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
        //}

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

        [HttpPost]
        [Route("create")]
        public IHttpActionResult Create(Booking booking)
        {
            // Constant for the initial safe booking duration (1.5 hours)
            const int DefaultDiningMinutes = 90;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // === Validate Table ===
            var table = db.Tables.FirstOrDefault(t => t.TableId == booking.TableId);
            if (table == null)
                return BadRequest("Invalid table.");

            // === Validate User ===
            var user = db.Users.FirstOrDefault(u => u.UserId == booking.UserId);
            if (user == null)
                return BadRequest("Invalid user.");
            if (booking.BookingDateTime.HasValue)
            {
                // If the client sends local time (3:46 PM) and you want to save as UTC
                // First specify it as Local, then convert to UTC
                var localTime = DateTime.SpecifyKind(booking.BookingDateTime.Value, DateTimeKind.Local);
                booking.BookingDateTime = localTime.ToUniversalTime();

                // Now when you read it back, convert from UTC to Local
                // var displayTime = booking.BookingDateTime.Value.ToLocalTime();
            }

            // ... (Existing Music, Coin, and Dedication Note Handling) ...

            // === Optional Music Handling (requires coins) ===
            if (booking.music_id.HasValue && booking.CoinCategoryIdUsedForMusic.HasValue)
            {
                var userCoin = db.CustomerCoins.FirstOrDefault(c =>
                     c.UserId == booking.UserId &&
                     c.CoinCategoryId == booking.CoinCategoryIdUsedForMusic);

                if (userCoin == null || userCoin.Balance < 10)
                    return BadRequest("Insufficient coins of selected category for music.");

                // Deduct 10 coins
                userCoin.Balance -= 10;
            }

            // === Check booking conflicts (Using Dynamic Logic) ===
            var requestedStart = booking.BookingDateTime ?? DateTime.MinValue;
            // Define the NEW booking window using the default duration
            var requestedEnd = requestedStart.AddMinutes(DefaultDiningMinutes);

            var bookingsForTable = db.Bookings
                .Where(b => b.TableId == table.TableId && b.Status == "Booked")
                .ToList();

            var overlappingBooking = bookingsForTable.FirstOrDefault(b =>
                // Check against EXISTING dynamic duration (b.MaxEstimatedMinutes)
                requestedStart < b.BookingDateTime.Value.AddMinutes(b.MaxEstimatedMinutes) &&
                requestedEnd > b.BookingDateTime.Value);

            if (overlappingBooking != null)
            {
                // Show available time using EXISTING dynamic duration
                var availableTime = overlappingBooking.BookingDateTime.Value.AddMinutes(overlappingBooking.MaxEstimatedMinutes);
                return BadRequest("This table is already booked during the selected time. " +
                                    $"Available after {availableTime:hh:mm tt}.");
            }

            // === Final booking setup ===
            booking.Status = "Booked";
            table.Status = "Booked";
            // *** Set the initial default duration on the NEW booking ***
            booking.MaxEstimatedMinutes = DefaultDiningMinutes;

            try
            {
                db.Bookings.Add(booking);
                db.SaveChanges(); // ✅ BookingId generated here
            }
            catch (Exception ex)
            {
                return BadRequest("Error saving booking: " + ex.Message +
                                    (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
            }

            // ... (Rest of the method: Jukebox and Waiter Assignment logic) ...
            if (booking.music_id.HasValue && booking.CoinCategoryIdUsedForMusic.HasValue)
            {
                var jukebox = new Jukebox
                {
                    UserId = booking.UserId,
                    BookingId = booking.BookingId,
                    MusicId = booking.music_id.Value,
                    CoinCategoryId = booking.CoinCategoryIdUsedForMusic,
                    CoinsSpent = 10,
                    DedicationNote = booking.DedicationNote,
                    RequestedAt = DateTime.Now
                };

                db.Jukeboxes.Add(jukebox);
                db.SaveChanges();
            }

            // === Waiter Assignment ===
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
                Message = "Booking created successfully with optional music and waiter assigned."
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
