using BooknowAPI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/order")]
    public class OrderController : ApiController
    {
        private newRestdbEntities7 db = new newRestdbEntities7();

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

        //    var orderItems = order.OrderItems.ToList();
        //    order.OrderItems = new List<OrderItem>();

        //    db.Orders.Add(order);
        //    db.SaveChanges(); // generate OrderId

        //    foreach (var item in orderItems)
        //    {
        //        var dish = db.Dishes.FirstOrDefault(d => d.DishId == item.DishId);
        //        if (dish == null)
        //            return BadRequest($"Dish not found (ID: {item.DishId})");

        //        // Case 1: Quantity > 1 and skipped ingredients provided → split
        //        if (item.Quantity > 1 && item.SkippedIngredients != null && item.SkippedIngredients.Any())
        //        {
        //            for (int i = 0; i < item.Quantity; i++)
        //            {
        //                var splitItem = new OrderItem
        //                {
        //                    OrderId = order.OrderId,
        //                    DishId = item.DishId,
        //                    Quantity = 1,
        //                    UnitPrice = (decimal)dish.Price,
        //                    SkippedIngredientIds = JsonConvert.SerializeObject(item.SkippedIngredients)
        //                };

        //                db.OrderItems.Add(splitItem);
        //                order.TotalPrice += splitItem.UnitPrice;

        //                // Assign chef here
        //                var chef = db.Users.FirstOrDefault(u => u.Role == "chef" &&
        //                            u.ChefDishSpecialities.Any(s => s.DishId == dish.DishId));
        //                if (chef != null)
        //                {
        //                    db.OrderChefAssignments.Add(new OrderChefAssignment
        //                    {
        //                        OrderId = order.OrderId,
        //                        DishId = dish.DishId,
        //                        ChefUserId = chef.UserId,
        //                        AssignedAt = DateTime.Now
        //                    });
        //                }
        //            }
        //        }
        //        else
        //        {
        //            // Case 2: Normal (no skipped or same skipped for all)
        //            item.OrderId = order.OrderId;
        //            item.UnitPrice = (decimal)(dish.Price * item.Quantity);

        //            if (item.SkippedIngredients != null && item.SkippedIngredients.Any())
        //                item.SkippedIngredientIds = JsonConvert.SerializeObject(item.SkippedIngredients);
        //            else
        //                item.SkippedIngredientIds = null;

        //            db.OrderItems.Add(item);
        //            order.TotalPrice += item.UnitPrice;

        //            // Assign chef here
        //            var chef = db.Users.FirstOrDefault(u => u.Role == "chef" &&
        //                        u.ChefDishSpecialities.Any(s => s.DishId == dish.DishId));
        //            if (chef != null)
        //            {
        //                db.OrderChefAssignments.Add(new OrderChefAssignment
        //                {
        //                    OrderId = order.OrderId,
        //                    DishId = dish.DishId,
        //                    ChefUserId = chef.UserId,
        //                    AssignedAt = DateTime.Now
        //                });
        //            }
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
            //adding extranotes as dedication note table mn column tha nam update ni kia uska bs 
            if (!string.IsNullOrWhiteSpace(order.DedicationNote))
            {
                order.DedicationNote = order.DedicationNote.Trim();
            }
            else
            {
                order.DedicationNote = null; // Set to null if empty
            }
            var orderItems = order.OrderItems.ToList();
            order.OrderItems = new List<OrderItem>();

            // Fetch all dish data including the new EstimatedMinutesToDine
            var allDishIds = orderItems.Select(oi => oi.DishId).Distinct().ToList();
            var dishesData = db.Dishes
                .Where(d => allDishIds.Contains(d.DishId))
                .Select(d => new { d.DishId, d.Price, d.EstimatedMinutesToDine })
                .ToList();
            
            db.Orders.Add(order);
            db.SaveChanges(); // generate OrderId

            int maxCalculatedMinutes = 0; // Tracks the longest dining time from the order

            foreach (var item in orderItems)
            {
                var dish = dishesData.FirstOrDefault(d => d.DishId == item.DishId);
                if (dish == null)
                    return BadRequest($"Dish not found (ID: {item.DishId})");

                // *** Calculate Max Duration ***
                // No casting needed as dish.EstimatedMinutesToDine is an int
                if (dish.EstimatedMinutesToDine > maxCalculatedMinutes)
                {
                    maxCalculatedMinutes = dish.EstimatedMinutesToDine;
                }

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

            db.SaveChanges(); // Save OrderItems and Assignments

            // *** CRITICAL UPDATE: Adjust Booking Duration ***
            // This logic unconditionally updates the booking duration to the calculated maximum time.
            if (maxCalculatedMinutes > 0) // Only update if dishes were actually found
            {
                // 🚨 UPDATE: We remove the 'greater than' check to allow the block time to shrink.
                booking.MaxEstimatedMinutes = maxCalculatedMinutes;

                db.Entry(booking).State = EntityState.Modified;
                db.SaveChanges(); // Persist the new, calculated duration to the database
            }

            return Ok(new     
               {
                   OrderId = order.OrderId,
                   BookingId = order.BookingId,
                   UserId = order.UserId,
                   // CRITICAL FIX: Use Convert.ToDouble() to guarantee standard number format for Swift.
                   TotalPrice = Convert.ToDouble(order.TotalPrice),
                   Status = order.Status,
                   DedicationNote = order.DedicationNote
            });
        }


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
            var orders = db.Orders.Where(o => o.UserId == userId)
                 .OrderByDescending(o => o.OrderId)   // 🔥 Latest order first
                 .Select(o => new
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
        public IHttpActionResult UpdateStatus(int id, [FromBody] StatusUpdateRequest statusRequest)
        {
            var order = db.Orders.Find(id);
            if (order == null)
                return NotFound();

            // Use status from the request body
            order.Status = statusRequest.Status;
            db.SaveChanges();

            return Ok(new
            {
                orderId = order.OrderId,
                status = order.Status,
                message = "Order status updated successfully"
            });
        }

        // Define a model to map the incoming JSON request
        public class StatusUpdateRequest
        {
            public string Status { get; set; }
        }

    }
}
