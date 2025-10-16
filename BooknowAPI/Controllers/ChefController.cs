using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using BooknowAPI.Models;


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
        [HttpGet]
        [Route("byid/{userId:int}")]
        public IHttpActionResult GetOrdersByChefId(int userId)
        {
            var chefOrders = db.OrderChefAssignments
                .Where(c => c.ChefUserId == userId
                         && c.Order.Status != "Completed"
                         && c.Order.Status != "Cancelled")
                .Select(c => new
                {
                    c.OrderId,

                    Order = new
                    {
                        OrderDate = c.Order != null ? c.Order.OrderDate : (DateTime?)null,
                        Status = c.Order != null ? c.Order.Status : null,

                        // ✅ include booking datetime
                        BookingDateTime = c.Order.Booking != null ? c.Order.Booking.BookingDateTime : (DateTime?)null,

                        Dishes = c.Order.OrderItems.Select(oi => new
                        {
                            DishId = oi.DishId,
                            DishName = oi.Dish != null ? oi.Dish.Name : null,
                            Quantity = oi.Quantity,
                            PrepTimeMinutes = oi.Dish != null ? oi.Dish.PrepTimeMinutes : (int?)null
                        })
                    }
                })
                .ToList();

            return Ok(chefOrders);
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