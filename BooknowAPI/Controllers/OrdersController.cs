using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using BooknowAPI.Models;
using Newtonsoft.Json;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/order")]
    public class OrderController : ApiController
    {
        private newRestdbEntities4 db = new newRestdbEntities4();

        // GET: api/order/all
        [HttpGet]
        [Route("all")]
        public IHttpActionResult GetAllOrders()
        {
            var orders = db.Orders.Select(o => new
            {
                o.OrderId,
                o.UserId,
                o.BookingId,
                o.OrderDate,
                o.TotalPrice,
                o.Status,
                Dishes = o.OrderItems.Select(oi => new
                {
                    oi.DishId,
                    DishName = oi.Dish.Name,
                    oi.Quantity,
                    oi.UnitPrice
                }),
                AssignedChefs = o.OrderChefAssignments.Select(ca => new
                {
                    ca.ChefUserId,
                    ChefName = ca.User.Name,
                    ca.DishId
                })
            }).ToList();

            return Ok(orders);
        }
        // POST: api/order/add
        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddOrder(Order order)
        {
            if (!ModelState.IsValid || order.OrderItems == null || !order.OrderItems.Any())
                return BadRequest("Invalid order payload.");

            var booking = db.Bookings.Find(order.BookingId);
            if (booking == null)
                return BadRequest("Booking not found.");

            order.OrderDate = DateTime.Now;
            order.Status = "Placed";
            order.TotalPrice = 0;

            var orderItems = order.OrderItems.ToList();
            order.OrderItems = new List<OrderItem>();

            db.Orders.Add(order);
            db.SaveChanges(); // generate OrderId

            foreach (var item in orderItems)
            {
                var dish = db.Dishes.FirstOrDefault(d => d.DishId == item.DishId);
                if (dish == null)
                    return BadRequest($"Dish not found (ID: {item.DishId})");

                // Case 1: Quantity > 1 and skipped ingredients provided → split
                if (item.Quantity > 1 && item.SkippedIngredients != null && item.SkippedIngredients.Any())
                {
                    for (int i = 0; i < item.Quantity; i++)
                    {
                        var splitItem = new OrderItem
                        {
                            OrderId = order.OrderId,
                            DishId = item.DishId,
                            Quantity = 1,
                            UnitPrice = (decimal)dish.Price,
                            SkippedIngredientIds = JsonConvert.SerializeObject(item.SkippedIngredients)
                        };

                        db.OrderItems.Add(splitItem);
                        order.TotalPrice += splitItem.UnitPrice;

                        // Assign chef here
                        var chef = db.Users.FirstOrDefault(u => u.Role == "chef" &&
                                    u.ChefDishSpecialities.Any(s => s.DishId == dish.DishId));
                        if (chef != null)
                        {
                            db.OrderChefAssignments.Add(new OrderChefAssignment
                            {
                                OrderId = order.OrderId,
                                DishId = dish.DishId,
                                ChefUserId = chef.UserId,
                                AssignedAt = DateTime.Now
                            });
                        }
                    }
                }
                else
                {
                    // Case 2: Normal (no skipped or same skipped for all)
                    item.OrderId = order.OrderId;
                    item.UnitPrice = (decimal)(dish.Price * item.Quantity);

                    if (item.SkippedIngredients != null && item.SkippedIngredients.Any())
                        item.SkippedIngredientIds = JsonConvert.SerializeObject(item.SkippedIngredients);
                    else
                        item.SkippedIngredientIds = null;

                    db.OrderItems.Add(item);
                    order.TotalPrice += item.UnitPrice;

                    // Assign chef here
                    var chef = db.Users.FirstOrDefault(u => u.Role == "chef" &&
                                u.ChefDishSpecialities.Any(s => s.DishId == dish.DishId));
                    if (chef != null)
                    {
                        db.OrderChefAssignments.Add(new OrderChefAssignment
                        {
                            OrderId = order.OrderId,
                            DishId = dish.DishId,
                            ChefUserId = chef.UserId,
                            AssignedAt = DateTime.Now
                        });
                    }
                }
            }

            db.SaveChanges();

            return Ok(new
            {
                order.OrderId,
                order.BookingId,
                order.UserId,
                order.TotalPrice,
                order.Status
            });
        }

        //// POST: api/order/add
        //[HttpPost]
        //[Route("add")]
        //public IHttpActionResult AddOrder(Order order)
        //{
        //    if (!ModelState.IsValid || order.OrderItems == null || !order.OrderItems.Any())
        //        return BadRequest("Invalid order payload.");

        //    var booking = db.Bookings.Find(order.BookingId);
        //    if (booking == null)
        //        return BadRequest("Booking not found.");

        //    order.OrderDate = DateTime.Now;
        //    order.Status = "Placed";
        //    order.TotalPrice = 0;

        //    // Temporarily store order items and clear to avoid EF issues
        //    var orderItems = order.OrderItems.ToList();
        //    order.OrderItems = new List<OrderItem>();

        //    db.Orders.Add(order);
        //    db.SaveChanges(); // Generate OrderId

        //    foreach (var item in orderItems)
        //    {
        //        var dish = db.Dishes.FirstOrDefault(d => d.DishId == item.DishId);
        //        if (dish == null)
        //            return BadRequest($"Dish not found (ID: {item.DishId})");

        //        item.OrderId = order.OrderId;
        //        item.UnitPrice = (decimal)(dish.Price * item.Quantity);

        //        // ✅ Save skipped ingredients if present
        //        if (item.SkippedIngredients != null && item.SkippedIngredients.Any())
        //        {
        //            item.SkippedIngredientIds = JsonConvert.SerializeObject(item.SkippedIngredients);
        //        }
        //        else
        //        {
        //            item.SkippedIngredientIds = null;
        //        }

        //        db.OrderItems.Add(item);
        //        order.TotalPrice += item.UnitPrice;

        //        // ✅ Assign chef based on dish speciality
        //        var chef = db.Users
        //            .FirstOrDefault(u => u.Role == "chef" &&
        //                                 u.ChefDishSpecialities.Any(s => s.DishId == dish.DishId));

        //        if (chef != null)
        //        {
        //            db.OrderChefAssignments.Add(new OrderChefAssignment
        //            {
        //                OrderId = order.OrderId,
        //                DishId = dish.DishId,
        //                ChefUserId = chef.UserId,
        //                AssignedAt = DateTime.Now
        //            });
        //        }
        //    }

        //    db.SaveChanges();

        //    return Ok(new
        //    {
        //        order.OrderId,
        //        order.BookingId,
        //        order.UserId,
        //        order.TotalPrice,
        //        order.Status
        //    });
        //}


        //// POST: api/order/add
        //[HttpPost]
        //[Route("add")]
        //public IHttpActionResult AddOrder(Order order)
        //{
        //    if (!ModelState.IsValid || order.OrderItems == null || !order.OrderItems.Any())
        //        return BadRequest("Invalid order payload.");

        //    var booking = db.Bookings.Find(order.BookingId);
        //    if (booking == null)
        //        return BadRequest("Booking not found.");

        //    order.OrderDate = DateTime.Now;
        //    order.Status = "Placed";
        //    order.TotalPrice = 0;

        //    // Temporarily store order items and clear to avoid EF issues
        //    var orderItems = order.OrderItems.ToList();
        //    order.OrderItems = new List<OrderItem>();

        //    db.Orders.Add(order);
        //    db.SaveChanges(); // Save to generate OrderId

        //    foreach (var item in orderItems)
        //    {
        //        var dish = db.Dishes.FirstOrDefault(d => d.DishId== item.DishId);
        //        if (dish == null)
        //            return BadRequest($"Dish not found (ID: {item.DishId})");

        //        item.OrderId = order.OrderId;
        //        item.UnitPrice = (decimal)(dish.Price * item.Quantity);

        //        db.OrderItems.Add(item);
        //        order.TotalPrice += item.UnitPrice;

        //        // Assign chef based on dish speciality
        //        var chef = db.Users
        //            .FirstOrDefault(u => u.Role == "chef" &&
        //                                 u.ChefDishSpecialities.Any(s => s.DishId == dish.DishId));

        //        if (chef != null)
        //        {
        //            db.OrderChefAssignments.Add(new OrderChefAssignment
        //            {
        //                OrderId = order.OrderId,
        //                DishId = dish.DishId,
        //                ChefUserId = chef.UserId,
        //                AssignedAt=DateTime.Now,
        //            });
        //        }
        //    }

        //    db.SaveChanges();

        //    return Ok(new
        //    {
        //        order.OrderId,
        //        order.BookingId,
        //        order.UserId,
        //        order.TotalPrice,
        //        order.Status
        //    });
        //}

        // PUT: api/order/cancel/{id}
        [HttpPut]
        [Route("cancel/{id:int}")]
        public IHttpActionResult CancelOrder(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null)
                return NotFound();

            var booking = db.Bookings.Find(order.BookingId);
            if (booking == null)
                return BadRequest("Booking not found for the order.");

            // Calculate time remaining (as TimeSpan)
            TimeSpan timeRemaining = (TimeSpan)(booking.BookingDateTime - DateTime.Now);

            // Ensure timeRemaining is not negative
            if (timeRemaining.TotalMinutes <= 0)
            {
                return Content(HttpStatusCode.Forbidden, "Booking time already passed or is too close to cancel.");
            }

            // Get DishIds from OrderItems
            var dishIds = db.OrderItems
                .Where(oi => oi.OrderId == order.OrderId)
                .Select(oi => oi.DishId)
                .ToList();

            // Get max prep time from those dishes
            int maxPrepTime = db.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .Select(d => (int?)d.PrepTimeMinutes)
                .Max() ?? 0;

            int minRequiredMinutes = maxPrepTime + 30;

            if (timeRemaining.TotalMinutes < minRequiredMinutes)
            {
                return Content(HttpStatusCode.Forbidden,
                    $"Cannot cancel. Only {Math.Floor(timeRemaining.TotalMinutes)} minutes left, but at least {minRequiredMinutes} minutes required.");
            }

            order.Status = "Cancelled";
            db.SaveChanges();

            return Ok(new { message = "Order cancelled successfully." });
        }

        // GET: api/order/byUser/{userId}
        [HttpGet]
        [Route("byUser/{userId:int}")]
        public IHttpActionResult GetOrdersByUser(int userId)
        {
            var orders = db.Orders.Where(o => o.UserId == userId).Select(o => new
            {
                o.OrderId,
                o.OrderDate,
                o.TotalPrice,
                o.Status,
                o.BookingId,
                Dishes = o.OrderItems.Select(oi => new
                {
                    oi.DishId,
                    DishName = oi.Dish.Name,
                    oi.Quantity
                })
            }).ToList();

            return Ok(orders);
        } 

        // DELETE: api/order/delete/{id}
        [HttpDelete]
        [Route("delete/{id:int}")]
        public IHttpActionResult DeleteOrder(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null)
                return NotFound();

            db.Orders.Remove(order);
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // PUT: api/order/status/{id}
        [HttpPut]
        [Route("status/{id:int}")]
        public IHttpActionResult UpdateStatus(int id, [FromBody] string status)
        {
            var order = db.Orders.Find(id);
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
