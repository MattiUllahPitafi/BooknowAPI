using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using BooknowAPI.Models;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/menu")]
    public class MenuController : ApiController
    {
        private newRestdbEntities db = new newRestdbEntities();

        // GET: api/menu/all
        [HttpGet]
        [Route("all")]
        public IHttpActionResult GetAllDishes()
        {
            var dishes = db.Dishes.Select(d => new
            {
                d.DishId,
                d.Name,
                d.Price,
                d.PrepTimeMinutes,
                d.RestaurantId,
                d.MenuCategoryId
            }).ToList();

            return Ok(dishes);
        }

        // GET: api/menu/filter?restaurantId=1&menuCategoryId=2&search=chicken
        [HttpGet]
        [Route("filter")]
        public IHttpActionResult FilterDishes(int? restaurantId = null, int? menuCategoryId = null, string search = null)
        {
            var query = db.Dishes.AsQueryable();

            if (restaurantId.HasValue)
                query = query.Where(d => d.RestaurantId == restaurantId);

            if (menuCategoryId.HasValue)
                query = query.Where(d => d.MenuCategoryId == menuCategoryId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(d => d.Name.Contains(search));

            var result = query.Select(d => new
            {
                d.DishId,
                d.Name,
                d.Price,
                d.PrepTimeMinutes,
                d.RestaurantId,
                d.MenuCategoryId
            }).ToList();

            return Ok(result);
        }

        // GET: api/menu/{id}
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetDishById(int id)
        {
            var dish = db.Dishes.Where(d => d.DishId == id).Select(d => new
            {
                d.DishId,
                d.Name,
                d.Price,
                d.PrepTimeMinutes,
                d.RestaurantId,
                d.MenuCategoryId
            }).FirstOrDefault();

            if (dish == null)
                return NotFound();

            return Ok(dish);
        }

        // POST: api/menu/add
        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddDish(Dish dish)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            db.Dishes.Add(dish);
            db.SaveChanges();

            return Ok(new
            {
                dish.DishId,
                dish.Name,
                dish.Price,
                dish.PrepTimeMinutes,
                dish.RestaurantId,
                dish.MenuCategoryId
            });
        }

        // PUT: api/menu/update/{id}
        [HttpPut]
        [Route("update/{id:int}")]
        public IHttpActionResult UpdateDish(int id, Dish updatedDish)
        {
            var dish = db.Dishes.Find(id);
            if (dish == null)
                return NotFound();

            dish.Name = updatedDish.Name;
            dish.Price = updatedDish.Price;
            dish.PrepTimeMinutes = updatedDish.PrepTimeMinutes;
            dish.RestaurantId = updatedDish.RestaurantId;
            dish.MenuCategoryId = updatedDish.MenuCategoryId;

            db.SaveChanges();

            return Ok(new
            {
                dish.DishId,
                dish.Name,
                dish.Price,
                dish.PrepTimeMinutes,
                dish.RestaurantId,
                dish.MenuCategoryId
            });
        }

        // DELETE: api/menu/delete/{id}
        [HttpDelete]
        [Route("delete/{id:int}")]
        public IHttpActionResult DeleteDish(int id)
        {
            var dish = db.Dishes.Find(id);
            if (dish == null)
                return NotFound();

            db.Dishes.Remove(dish);
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}
