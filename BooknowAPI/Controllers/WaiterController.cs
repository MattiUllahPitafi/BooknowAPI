using BooknowAPI.Models;
using System;
using System.Linq;
using System.Web.Http;

namespace RestWAdvBook.Controllers
{
    [RoutePrefix("api/waiters")]
    public class WaitersController : ApiController
    {
        private newRestdbEntities7 db = new newRestdbEntities7();


        //[HttpGet]
        //[Route("byid/{waiterId:int}")]
        //public IHttpActionResult GetAssignmentsByWaiter(int waiterId)
        //{
        //    var assignments = db.WaiterAssignments
        //        .Where(a => a.WaiterUserId == waiterId)
        //        .Join(db.Orders,
        //            a => a.BookingId,
        //            o => o.BookingId,
        //            (a, o) => new { a, o })
        //        .Where(joined => joined.o.Status.ToLower() == "inprogress" || joined.o.Status.ToLower() == "completed")
        //        .Select(joined => new
        //        {
        //            waiterUserId = joined.a.WaiterUserId ?? 0,
        //            bookingId = joined.a.BookingId ?? 0,
        //            orderId = joined.o.OrderId, // Add OrderId here
        //            booking = new
        //            {
        //                tableId = joined.a.Booking.TableId,
        //                table = new
        //                {
        //                    name = joined.a.Booking.Table.Name,
        //                    floor = joined.a.Booking.Table.Floor

        //                }
        //            },
        //            orderStatus = joined.o.Status,  // Add order status
        //        })
        //        .ToList();

        //    if (!assignments.Any())
        //        return NotFound();

        //    return Ok(assignments);
        //}
        [HttpGet]
        [Route("byid/{waiterId:int}")]
        public IHttpActionResult GetAssignmentsByWaiter(int waiterId)
        {
            var assignments = db.WaiterAssignments
                .Where(a => a.WaiterUserId == waiterId)
                .Join(db.Orders,
                    a => a.BookingId,
                    o => o.BookingId,
                    (a, o) => new { a, o })
                .Where(joined => joined.o.Status.ToLower() == "inprogress" || joined.o.Status.ToLower() == "completed")
                .GroupJoin(db.Jukeboxes,  // Left join with Jukebox
                    joined => joined.a.BookingId,
                    j => j.BookingId,
                    (joined, jukeboxes) => new { joined.a, joined.o, jukeboxes })
                .SelectMany(
                    x => x.jukeboxes.DefaultIfEmpty(),  // This creates the left join
                    (x, j) => new { x.a, x.o, j })
                .Select(joined => new
                {
                    waiterUserId = joined.a.WaiterUserId ?? 0,
                    bookingId = joined.a.BookingId ?? 0,
                    orderId = joined.o.OrderId,
                    booking = new
                    {
                        tableId = joined.a.Booking.TableId,
                        table = new
                        {
                            name = joined.a.Booking.Table.Name,
                            floor = joined.a.Booking.Table.Floor,
                            dedication = joined.j != null ? joined.j.DedicationNote : null  // Handle null jukebox
                        }
                    },
                    orderStatus = joined.o.Status,
                })
                .ToList();

            if (!assignments.Any())
                return NotFound();

            return Ok(assignments);
        }

        [HttpGet]
        [Route("debug/{waiterId:int}")]
        public IHttpActionResult DebugWaiterAssignments(int waiterId)
        {
            // Check all assignments for this waiter
            var allAssignments = db.WaiterAssignments
                .Where(a => a.WaiterUserId == waiterId)
                .Select(a => new
                {
                    a.BookingId,
                    a.WaiterUserId
                })
                .ToList();

            // Check Jukebox entries for these bookings
            var assignmentsWithJukebox = db.WaiterAssignments
                .Where(a => a.WaiterUserId == waiterId)
                .Join(db.Jukeboxes,
                    a => a.BookingId,
                    j => j.BookingId,
                    (a, j) => new
                    {
                        BookingId = a.BookingId,
                        Dedication = j.DedicationNote,
                        WaiterId = a.WaiterUserId
                    })
                .ToList();

            return Ok(new
            {
                AllAssignments = allAssignments,
                AssignmentsWithJukebox = assignmentsWithJukebox
            });
        }
        // PUT: api/waiters/serve/{orderId}
        [HttpPut]
        [Route("serve/{orderId:int}")]
        public IHttpActionResult ServeOrder(int orderId)
        {
            // Fetch the order by its OrderId
            var order = db.Orders.FirstOrDefault(o => o.OrderId == orderId);

            // If the order is not found, return a 404 Not Found response
            if (order == null)
            {
                return NotFound();
            }

            // Update the status to 'Served'
            order.Status = "Served";

            try
            {
                // Save the changes to the database
                db.SaveChanges();

                // Return success message with the updated status
                return Ok(new
                {
                    message = "Order status updated to 'Served'",
                    orderId = order.OrderId,
                    status = order.Status
                });
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during saving the changes
                return InternalServerError(ex);
            }
        }

     

    }

}
