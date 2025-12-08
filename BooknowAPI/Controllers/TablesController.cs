using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BooknowAPI.Models;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/tables")]

    public class TablesController : ApiController
    {
        private newRestdbEntities7 db = new newRestdbEntities7();

        // GET: api/tables
        [HttpGet]
        [Route("all")]
        public IHttpActionResult GetTables()
        {
            var tables = db.Tables.Select(r => new
            {
                r.Name,
                r.Location,
                r.Floor,
                r.Price,
                r.Status,
            });
            return Ok(tables);
        }
        // GET: api/tables/5
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetTable(int id)
        {
            var tables = db.Tables.Find(id);
            if (tables == null)
                return NotFound();

            return Ok(new
            {
               tables.Name,
               tables.Location,
               tables.Floor,
               tables.Price,
               tables.Status,
            });

        }
        // GET: api/tables/restaurant/3
        [HttpGet]
        [Route("restaurant/{restaurantId:int}")]
        public IHttpActionResult GetTablesByRestaurant(int restaurantId)
        {
            var tables = db.Tables
                           .Where(t => t.RestaurantId == restaurantId)
                           .Select(t => new
                           {
                               t.TableId,
                               t.Name,
                               t.Location,
                               t.Floor,
                               t.Price,
                               t.Status,
                               t.Capacity,
                               t.RestaurantId
                           })
                           .ToList();

            if (!tables.Any())
                return NotFound();

            return Ok(tables);
        }
        // GET: api/tables/available/{restaurantId}?datetime=2025-08-04T22:00:00
        [HttpGet]
        [Route("available/{restaurantId:int}")]

        //public IHttpActionResult GetAvailableTablesByRestaurantAndTime(int restaurantId, DateTime datetime, int? floor = null)
        //{
        //    var bookedTableIds = db.Bookings
        //        .Where(b => b.Table.RestaurantId == restaurantId &&
        //                    b.Status == "Booked" &&
        //                    datetime >= DbFunctions.AddMinutes(b.BookingDateTime, -100) &&
        //                    datetime <= DbFunctions.AddMinutes(b.BookingDateTime, 100))
        //        .Select(b => b.TableId)
        //        .ToList();

        //    var query = db.Tables
        //        .Where(t => t.RestaurantId == restaurantId);

        //    if (floor.HasValue)
        //    {
        //        query = query.Where(t => t.Floor == floor.Value);
        //    }

        //    var tables = query
        //        .Select(t => new
        //        {
        //            t.TableId,
        //            t.Name,
        //            t.Location,
        //            t.Floor,
        //            t.Price,
        //            Status = bookedTableIds.Contains(t.TableId) ? "Booked" : "Available",
        //            t.Capacity,
        //            t.RestaurantId
        //        })
        //        .ToList();

        //    return Ok(tables);
        //}

        //public IHttpActionResult GetAvailableTablesByRestaurantAndTime(int restaurantId, DateTime datetime, int? floor = null)
        //{
        //    // Booking window = 2 hours
        //    int duration = 2;
        //    var requestedStart = datetime;
        //    var requestedEnd = datetime.AddHours(duration);

        //    // Find tables that overlap with the requested time
        //    var bookedTableIds = db.Bookings
        //        .Where(b =>
        //            b.Table.RestaurantId == restaurantId &&
        //            b.Status == "Booked" &&
        //            requestedStart < DbFunctions.AddHours(b.BookingDateTime, duration) &&
        //            requestedEnd > b.BookingDateTime
        //        )
        //        .Select(b => b.TableId)
        //        .ToList();

        //    // Filter tables
        //    var query = db.Tables.Where(t => t.RestaurantId == restaurantId);

        //    if (floor.HasValue)
        //        query = query.Where(t => t.Floor == floor);

        //    var tables = query
        //        .Select(t => new
        //        {
        //            t.TableId,
        //            t.Name,
        //            t.Location,
        //            t.Floor,
        //            t.Price,
        //            Status = bookedTableIds.Contains(t.TableId) ? "Booked" : "Available",
        //            t.Capacity,
        //            t.RestaurantId
        //        })
        //        .ToList();

        //    return Ok(tables);
        //}correct tha but estimated food k hisab sy nechy kr rha hon

        //public IHttpActionResult GetAvailableTablesByRestaurantAndTime(int restaurantId, DateTime datetime, int? floor = null)
        //{
        //    // The duration the NEW user is looking for (90 minutes) must match the initial 
        //    // block duration set in the Create(Booking) function.
        //    const int RequestedDurationMinutes = 90;

        //    var requestedStart = datetime;
        //    // Define the NEW requested window based on the 90-minute minimum commitment
        //    var requestedEnd = datetime.AddMinutes(RequestedDurationMinutes);

        //    // Find tables that overlap with the requested 90-minute slot
        //    var bookedTableIds = db.Bookings
        //        .Where(b =>
        //            b.Table.RestaurantId == restaurantId &&
        //            b.Status == "Booked" &&

        //            // Overlap Check: Uses b.MaxEstimatedMinutes for all EXISTING bookings.
        //            // This works because:
        //            // 1. Old bookings/No Order: b.MaxEstimatedMinutes is 90 (DB default).
        //            // 2. Bookings With Order: b.MaxEstimatedMinutes is the dynamic calculated time.

        //            // Condition 1: Requested 90-min start must be before EXISTING booking end
        //            requestedStart < DbFunctions.AddMinutes(b.BookingDateTime, b.MaxEstimatedMinutes) &&

        //            // Condition 2: Requested 90-min end must be after EXISTING booking start
        //            requestedEnd > b.BookingDateTime
        //        )
        //        .Select(b => b.TableId)
        //        .ToList();

        //    // ----------------------------------------------------------------------

        //    // Filter tables
        //    var query = db.Tables.Where(t => t.RestaurantId == restaurantId);

        //    if (floor.HasValue)
        //        query = query.Where(t => t.Floor == floor);

        //    var tables = query
        //        .Select(t => new
        //        {
        //            t.TableId,
        //            t.Name,
        //            t.Location,
        //            t.Floor,
        //            t.Price,
        //            // Status is correctly set based on the calculated conflict list
        //            Status = bookedTableIds.Contains(t.TableId) ? "Booked" : "Available",
        //            t.Capacity,
        //            t.RestaurantId
        //        })
        //        .ToList();

        //    return Ok(tables);
        //}kam kr rha h
       
    public IHttpActionResult GetAvailableTablesByRestaurantAndTime(
    int restaurantId,
    DateTime datetime,
    int? floor = null)
        {
            const int RequestedDurationMinutes = 90;

            // === 🔒 FORCE UTC ===
            var requestedStart =
     datetime.Kind == DateTimeKind.Utc
         ? datetime
         : DateTime.SpecifyKind(datetime, DateTimeKind.Local).ToUniversalTime();


            var requestedEnd = requestedStart.AddMinutes(RequestedDurationMinutes);

            var bookedTableIds = db.Bookings
                .Where(b =>
                    b.Table.RestaurantId == restaurantId &&
                    b.Status == "Booked" &&
                    requestedStart < DbFunctions.AddMinutes(b.BookingDateTime, b.MaxEstimatedMinutes) &&
                    requestedEnd > b.BookingDateTime
                )
                .Select(b => b.TableId)
                .ToList();

            var query = db.Tables.Where(t => t.RestaurantId == restaurantId);

            if (floor.HasValue)
                query = query.Where(t => t.Floor == floor);

            var tables = query
                .Select(t => new
                {
                    t.TableId,
                    t.Name,
                    t.Location,
                    t.Floor,
                    t.Price,
                    Status = bookedTableIds.Contains(t.TableId) ? "Booked" : "Available",
                    t.Capacity,
                    t.RestaurantId
                })
                .ToList();

            return Ok(tables);
        }



    }
}
