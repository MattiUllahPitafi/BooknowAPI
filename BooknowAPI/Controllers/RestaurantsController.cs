using BooknowAPI.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/restaurants")]
    public class RestaurantsController : ApiController
    {
        private newRestdbEntities7 db = new newRestdbEntities7();

        // GET: api/restaurants
        [HttpGet]
        [Route("all")]
        public IHttpActionResult GetRestaurants()
        {
            var restaurants = db.Restaurants.Select( r=> new
            {
                r.Name,
                r.Location,
                r.Category,
                r.ImageUrl,
                r.RestaurantId
            });
            return Ok(restaurants);
        }

        // GET: api/restaurants/5
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetRestaurant(int id)
        {
            var restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
                return NotFound();

            return Ok(new
            {
                restaurant.Name,
                restaurant.Location,
                restaurant.Category,
                restaurant.ImageUrl,
                restaurant.RestaurantId
            });

        }

        // POST: api/restaurants/create
        [HttpPost]
        [Route("create")]
        public IHttpActionResult CreateRestaurant(Restaurant restaurant)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            db.Restaurants.Add(restaurant);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = restaurant.RestaurantId }, restaurant);
        }

        // PUT: api/restaurants/update/5
        [HttpPut]
        [Route("update/{id:int}")]
        public IHttpActionResult UpdateRestaurant(int id, Restaurant restaurant)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = db.Restaurants.Find(id);
            if (existing == null)
                return NotFound();

            existing.Name = restaurant.Name;
            existing.Location = restaurant.Location;
            existing.Category = restaurant.Category;
            existing.ImageUrl = restaurant.ImageUrl;

            db.Entry(existing).State = EntityState.Modified;
            db.SaveChanges();

            return Ok(existing);
        }

        // DELETE: api/restaurants/delete/5
        [HttpDelete]
        [Route("delete/{id:int}")]
        public IHttpActionResult DeleteRestaurant(int id)
        {
            var restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
                return NotFound();

            db.Restaurants.Remove(restaurant);
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
