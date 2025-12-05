using BooknowAPI.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/restaurants")]
    public class RestaurantsController : ApiController
    {
        private newRestdbEntities7 db = new newRestdbEntities7();

        [HttpGet]
        [Route("allR")]
        public IHttpActionResult GetRestaurants1()
        {
            try
            {
                var restaurants = db.Restaurants
                    .GroupJoin(
                        db.Ratings.Where(r => r.Stars != null),
                        r => r.RestaurantId,
                        rating => rating.RestaurantId,
                        (restaurant, ratings) => new
                        {
                            restaurant.Name,
                            restaurant.Location,
                            restaurant.Category,
                            restaurant.ImageUrl,
                            restaurant.RestaurantId,
                            AverageRating = ratings.Any() ?
                                ratings.Average(r => (double)r.Stars) : 0.0,
                            ReviewCount = ratings.Count()
                        })
                    .ToList();

                return Ok(restaurants);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetRestaurants1: {ex.Message}");

                // If the issue is with casting, try this alternative:
                try
                {
                    var restaurants = db.Restaurants
                        .Select(r => new
                        {
                            r.Name,
                            r.Location,
                            r.Category,
                            r.ImageUrl,
                            r.RestaurantId
                        })
                        .ToList();

                    // Get ratings separately
                    var restaurantIds = restaurants.Select(r => r.RestaurantId).ToList();
                    var ratingsDict = db.Ratings
                        .Where(r => r.Stars != null && restaurantIds.Contains((int)r.RestaurantId))
                        .GroupBy(r => r.RestaurantId)
                        .Select(g => new
                        {
                            RestaurantId = g.Key,
                            AverageRating = g.Average(r => Convert.ToDouble(r.Stars)),
                            ReviewCount = g.Count()
                        })
                        .ToDictionary(r => r.RestaurantId);

                    var result = restaurants.Select(r => new
                    {
                        r.Name,
                        r.Location,
                        r.Category,
                        r.ImageUrl,
                        r.RestaurantId,
                        AverageRating = ratingsDict.ContainsKey(r.RestaurantId) ?
                            ratingsDict[r.RestaurantId].AverageRating : 0.0,
                        ReviewCount = ratingsDict.ContainsKey(r.RestaurantId) ?
                            ratingsDict[r.RestaurantId].ReviewCount : 0
                    }).ToList();

                    return Ok(result);
                }
                catch (Exception ex2)
                {
                    return InternalServerError(new Exception($"Primary error: {ex.Message}. Secondary error: {ex2.Message}"));
                }
            }
        }
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
        [HttpPost]
        [Route("create")]
        public IHttpActionResult CreateRestaurant()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest == null || httpRequest.Form.Count == 0)
                    return BadRequest("No form data received.");

                // ------------------------------
                // 1️⃣ Parse and validate fields
                // ------------------------------
                string name = (httpRequest["Name"] ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest("Restaurant name is required.");

                string location = (httpRequest["Location"] ?? "").Trim();
                if (string.IsNullOrWhiteSpace(location))
                    return BadRequest("Location is required.");

                string category = (httpRequest["Category"] ?? "").Trim();
                if (string.IsNullOrWhiteSpace(category))
                    return BadRequest("Category is required.");

                // ------------------------------
                // 2️⃣ Handle optional image upload
                // ------------------------------
                string imageUrl = null;
                if (httpRequest.Files.Count > 0)
                {
                    var postedFile = httpRequest.Files["Image"] ?? httpRequest.Files[0];
                    if (postedFile != null && postedFile.ContentLength > 0)
                    {
                        string folderPath = @"C:\images";
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(postedFile.FileName)}";
                        string fullPath = Path.Combine(folderPath, fileName);
                        postedFile.SaveAs(fullPath);

                        imageUrl = $"images/{fileName}";
                    }
                }

                // ------------------------------
                // 3️⃣ Create Restaurant
                // ------------------------------
                var restaurant = new Restaurant
                {
                    Name = name,
                    Location = location,
                    Category = category,
                    ImageUrl = imageUrl,
                    ImageBase64 = null // Not used anymore
                };

                db.Restaurants.Add(restaurant);
                db.SaveChanges();

                return CreatedAtRoute("DefaultApi", new { id = restaurant.RestaurantId }, restaurant);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        


        //// POST: api/restaurants/create
        //[HttpPost]
        //[Route("create")]
        //public IHttpActionResult CreateRestaurant(Restaurant restaurant)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    db.Restaurants.Add(restaurant);
        //    db.SaveChanges();

        //    return CreatedAtRoute("DefaultApi", new { id = restaurant.RestaurantId }, restaurant);
        //}

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
