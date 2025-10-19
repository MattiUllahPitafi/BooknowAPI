using BooknowAPI.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;


namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/cheforder")]

    public class ChefController : ApiController
    {
        private newRestdbEntities7 db = new newRestdbEntities7();

        //// GET: api/cheforder/byid?userid=5
        //[HttpGet]
        //[Route("byid/{userId:int}")]
        //public IHttpActionResult GetOrdersByChefId(int userid)
        //{
        //    var chefOrders = db.OrderChefAssignments
        //        .Where(c => c.ChefUserId == userid)
        //        .Select(c => new
        //        {
        //            c.OrderId,

        //            Order = new
        //            {
        //                OrderDate = c.Order != null ? c.Order.OrderDate : (DateTime?)null,
        //                Status = c.Order != null ? c.Order.Status : null,


        //                Dishes = c.Order.OrderItems
        //                    .Select(oi => new
        //                    {
        //                        DishId= oi.DishId,
        //                        DishName = oi.Dish != null ? oi.Dish.Name : null,
        //                        Quantity = oi.Quantity,
        //                    })
        //            }
        //        })
        //        .ToList();

        //    return Ok(chefOrders);
        //}
        // GET: api/cheforder/byid/{userId}
        //[HttpGet]
        //[Route("byid/{userId:int}")]
        //public IHttpActionResult GetOrdersByChefId(int userId)
        //{
        //    // Step 1: Include related data
        //    var chefAssignments = db.OrderChefAssignments
        //        .Where(c => c.ChefUserId == userId
        //                 && c.Order.Status != "Completed"
        //                 && c.Order.Status != "Cancelled")
        //        .Include("Order.OrderItems.Dish")
        //        .Include("Order.Booking")
        //        .ToList(); // Move to memory

        //    // Step 2: Load ingredients once
        //    var ingredients = db.Ingredients.ToList();

        //    // Step 3: Build exact JSON structure for Swift
        //    var result = chefAssignments.Select(c => new
        //    {
        //        c.OrderId,
        //        order = new
        //        {
        //            orderDate = c.Order.OrderDate,
        //            status = c.Order.Status,
        //            bookingDateTime = c.Order.Booking != null
        //                ? (DateTime?)c.Order.Booking.BookingDateTime
        //                : null,

        //            dishes = c.Order.OrderItems.Select(oi => new
        //            {
        //                orderItemId = oi.OrderItemId,
        //                dishId = oi.DishId,
        //                dishName = oi.Dish != null ? oi.Dish.Name : null,
        //                quantity = oi.Quantity,
        //                prepTimeMinutes = oi.Dish?.PrepTimeMinutes,

        //                // ✅ Parse and map skipped ingredients correctly
        //                skippedIngredients = !string.IsNullOrEmpty(oi.SkippedIngredientIds)
        //                    ? oi.SkippedIngredientIds
        //                        .Replace("[", "")
        //                        .Replace("]", "")
        //                        .Split(',')
        //                        .Select(idStr => idStr.Trim())
        //                        .Where(idStr => int.TryParse(idStr, out _))
        //                        .Select(idStr =>
        //                        {
        //                            int id = int.Parse(idStr);
        //                            return ingredients.FirstOrDefault(i => i.IngredientId == id)?.Name;
        //                        })
        //                        .Where(name => name != null)
        //                        .ToList()
        //                    : new List<string>()
        //            }).ToList()
        //        }
        //    }).ToList();

        //    return Ok(result);
        //}

        [HttpGet]
        [Route("byid/{userId:int}")]
        public IHttpActionResult GetOrdersByChefId(int userId)
        {
            // Use server local "today" (00:00:00) and tomorrow (00:00:00)
            var todayStart = DateTime.Today;
            var tomorrowStart = todayStart.AddDays(1);

            // Step 1: Try to get today's orders (BookingDateTime in [todayStart, tomorrowStart))
            var chefAssignments = db.OrderChefAssignments
                .Where(c => c.ChefUserId == userId
                         && c.Order.Status != "Completed"
                         && c.Order.Status != "Cancelled"
                         && c.Order.Booking != null
                         && c.Order.Booking.BookingDateTime >= todayStart
                         && c.Order.Booking.BookingDateTime < tomorrowStart)
                .Include("Order.OrderItems.Dish")
                .Include("Order.Booking")
                .ToList();

            // Step 2: If none found for today, find the next booking datetime (after now) and load that day's orders
            if (!chefAssignments.Any())
            {
                // Find the earliest booking datetime strictly after now
                var nextBookingDateTime = db.Bookings
                    .Where(b => b.BookingDateTime != null && b.BookingDateTime > DateTime.Now)
                    .OrderBy(b => b.BookingDateTime)
                    .Select(b => b.BookingDateTime)
                    .FirstOrDefault();

                if (nextBookingDateTime != null && nextBookingDateTime != default(DateTime))
                {
                    var nextDayStart = nextBookingDateTime.Value.Date;
                    var nextDayEnd = nextDayStart.AddDays(1);

                    chefAssignments = db.OrderChefAssignments
                        .Where(c => c.ChefUserId == userId
                                 && c.Order.Status != "Completed"
                                 && c.Order.Status != "Cancelled"
                                 && c.Order.Booking != null
                                 && c.Order.Booking.BookingDateTime >= nextDayStart
                                 && c.Order.Booking.BookingDateTime < nextDayEnd)
                        .Include("Order.OrderItems.Dish")
                        .Include("Order.Booking")
                        .ToList();
                }
            }

            // Load ingredients once for lookup in-memory
            var ingredients = db.Ingredients.ToList();

            // Build response matching Swift shape (order wrapped under "order")
            var result = chefAssignments
                .Select(c => new
                {
                    c.OrderId,
                    order = new
                    {
                        orderDate = c.Order.OrderDate,
                        status = c.Order.Status,
                        bookingDateTime = c.Order.Booking != null
                            ? (DateTime?)c.Order.Booking.BookingDateTime
                            : null,

                        dishes = c.Order.OrderItems.Select(oi => new
                        {
                            orderItemId = oi.OrderItemId,
                            dishId = oi.DishId,
                            dishName = oi.Dish?.Name,
                            quantity = oi.Quantity,
                            prepTimeMinutes = oi.Dish?.PrepTimeMinutes,

                            skippedIngredients = !string.IsNullOrEmpty(oi.SkippedIngredientIds)
                                ? oi.SkippedIngredientIds
                                    .Replace("[", "")
                                    .Replace("]", "")
                                    .Split(',')
                                    .Select(idStr => idStr.Trim())
                                    .Where(idStr => int.TryParse(idStr, out _))
                                    .Select(idStr =>
                                    {
                                        int id = int.Parse(idStr);
                                        return ingredients.FirstOrDefault(i => i.IngredientId == id)?.Name;
                                    })
                                    .Where(name => name != null)
                                    .ToList()
                                : new List<string>()
                        }).ToList()
                    }
                })
                // Sort by bookingDateTime ascending (nulls will go first — if you want them last, use OrderBy(o => o.order.bookingDateTime == null).ThenBy(o => o.order.bookingDateTime))
                .OrderBy(o => o.order.bookingDateTime)
                .ToList();

            return Ok(result);
        }


        [HttpPut]
[Route("status/{id}")]
public IHttpActionResult UpdateStatus(int id, [FromBody] string status)
{
    if (string.IsNullOrWhiteSpace(status))
        return BadRequest("Status is required.");

    var order = db.Orders.FirstOrDefault(o => o.OrderId == id);
    if (order == null)
        return NotFound();

    order.Status = status;
    db.SaveChanges();

    return Ok(new
    {
        orderId = order.OrderId,
        status = order.Status,
        message = "Order status updated successfully"
    });
}



    }
}