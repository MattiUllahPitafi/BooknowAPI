using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using BooknowAPI.Models;
using Newtonsoft.Json.Linq;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/Users")]
    public class UsersController : ApiController
    {
        private newRestdbEntities2 db = new newRestdbEntities2();

        // GET: api/Users/GetAll
        [HttpGet]
        [Route("GetAll")]
        public IHttpActionResult GetAll()
        {
            var users = db.Users.Select(u => new
            {
                u.UserId,
                u.Name,
                u.Email,
                u.Role
            }).ToList();

            return Ok(users);
        }

        // GET: api/Users/Get/5
        [HttpGet]
        [Route("Get/{id}")]
        public IHttpActionResult Get(int id)
        {
            var user = db.Users.FirstOrDefault(u => u.UserId == id);

            if (user == null)
                return NotFound();

            object response;

            if (user.Role == "Customer")
            {
                var allCategories = db.CoinCategories.ToList();

                var userCoins = db.CustomerCoins
                                  .Where(c => c.UserId == id)
                                  .ToList();

                var coinsWithCategories = allCategories.Select(cat => new
                {
                    CategoryId = cat.CoinCategoryId,
                    CategoryName = cat.Name,
                    Balance = userCoins
                                .Where(uc => uc.CoinCategoryId == cat.CoinCategoryId)
                                .Select(uc => uc.Balance)
                                .FirstOrDefault() // Will be 0 if no match
                }).ToList();

                response = new
                {
                    user.UserId,
                    user.Name,
                    user.Email,
                    user.Role,
                    Coins = coinsWithCategories
                };
            }
            else
            {
                response = new
                {
                    user.UserId,
                    user.Name,
                    user.Email,
                    user.Role
                };
            }

            return Ok(response);
        }


        // POST: api/Users/Register (Only for customers)
        [HttpPost]
        [Route("Register")]
        public IHttpActionResult Register([FromBody] User user)
        {
            if (!ModelState.IsValid || user == null)
                return BadRequest("Invalid data");

            if (db.Users.Any(u => u.Email == user.Email))
                return BadRequest("Email already exists");

            user.Role = "Customer";
            user.PasswordHash = user.PasswordHash ?? "123456"; // Default password if null (or handle as needed)
            db.Users.Add(user);
            db.SaveChanges();

            AssignInitialCoins(user.UserId);

            return Ok(new { Message = "Customer registered and coins assigned.", user.UserId });
        }

        // POST: api/Users/CreateStaff (For Admin to add Chefs or Waiters)
        [HttpPost]
        [Route("CreateChef")]
        public IHttpActionResult CreateChef([FromBody] JObject data)
        {
            if (data == null)
                return BadRequest("Invalid data.");

            var userJson = data["user"];
            var restaurantId = data["restaurantId"]?.ToObject<int>() ?? 0;
            var dishIds = data["dishIds"]?.ToObject<List<int>>();

            if (userJson == null || restaurantId == 0 || dishIds == null || !dishIds.Any())
                return BadRequest("Missing required fields.");

            var user = userJson.ToObject<User>();

            if (user.Role != "Chef")
                return BadRequest("Only 'Chef' role is allowed.");

            if (string.IsNullOrEmpty(user.PasswordHash))
                return BadRequest("Password is required.");

            // Add user to Users table
            db.Users.Add(user);
            db.SaveChanges();

            // Add chef to Chefs table
            var chef = new Chef
            {
                UserId = user.UserId,
                RestaurantId = restaurantId
            };
            db.Chefs.Add(chef);
            db.SaveChanges();

            // Add specialities to ChefDishSpecialities table
            foreach (var dishId in dishIds)
            {
                var speciality = new ChefDishSpeciality
                {
                    UserId = user.UserId,
                    DishId = dishId
                };
                db.ChefDishSpecialities.Add(speciality);
            }
            db.SaveChanges();

            return Ok(new
            {
                Message = "Chef created with specialities.",
                ChefId = user.UserId,
                RestaurantId = restaurantId,
                Specialities = dishIds
            });
        }
        // POST: api/Users/CreateWaiter
        [HttpPost]
        [Route("CreateWaiter")]
        public IHttpActionResult CreateWaiter([FromBody] JObject data)
        {
            if (data == null)
                return BadRequest("Invalid data.");

            var userJson = data["user"];
            var restaurantId = data["restaurantId"]?.ToObject<int>() ?? 0;

            if (userJson == null || restaurantId == 0)
                return BadRequest("Missing required fields.");

            var user = userJson.ToObject<User>();

            if (user.Role != "Waiter")
                return BadRequest("Role must be 'Waiter'.");

            if (string.IsNullOrEmpty(user.PasswordHash))
                return BadRequest("Password is required.");

            // Check for duplicate email
            if (db.Users.Any(u => u.Email == user.Email))
                return BadRequest("Email already exists.");

            // Add user to Users table
            db.Users.Add(user);
            db.SaveChanges();

            // Add waiter to Waiters table
            var waiter = new Waiter
            {
                UserId = user.UserId,
                RestaurantId = restaurantId
            };
            db.Waiters.Add(waiter);
            db.SaveChanges();

            return Ok(new
            {
                Message = "Waiter created successfully.",
                WaiterId = user.UserId,
                RestaurantId = restaurantId
            });
        }


        // POST: api/Users/Login
        [HttpPost]
        [Route("Login")]
        public IHttpActionResult Login([FromBody] User login)
        {
            if (string.IsNullOrEmpty(login.Email) || string.IsNullOrEmpty(login.PasswordHash))
                return BadRequest("Email and password required");

            var user = db.Users.FirstOrDefault(u => u.Email == login.Email && u.PasswordHash == login.PasswordHash);

            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                user.UserId,
                user.Name,
                user.Email,
                user.Role
            });
        }

        // PUT: api/Users/Update/5
        [HttpPut]
        [Route("Update/{id}")]
        public IHttpActionResult Update(int id, [FromBody] User updatedUser)
        {
            var existing = db.Users.Find(id);
            if (existing == null)
                return NotFound();

            existing.Name = updatedUser.Name;
            existing.Email = updatedUser.Email;
            existing.PasswordHash = updatedUser.PasswordHash;
            db.SaveChanges();

            return Ok(new { Message = "User updated successfully" });
        }

        // DELETE: api/Users/Delete/5
        [HttpDelete]
        [Route("Delete/{id}")]
        public IHttpActionResult Delete(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return NotFound();

            db.Users.Remove(user);
            db.SaveChanges();

            return Ok(new { Message = "User deleted" });
        }

        // Assign default coins
        private void AssignInitialCoins(int userId)
        {
            var coins = new[]
            {
                new { CategoryName = "Gold", Balance = 100 },
                new { CategoryName = "Platinum", Balance = 50 },
                new { CategoryName = "Diamond", Balance = 20 }
            };

            foreach (var coin in coins)
            {
                var category = db.CoinCategories.FirstOrDefault(c => c.Name == coin.CategoryName);
                if (category != null)
                {
                    db.CustomerCoins.Add(new CustomerCoin
                    {
                        UserId = userId,
                        CoinCategoryId = category.CoinCategoryId,
                        Balance = coin.Balance
                    });
                }
            }

            db.SaveChanges();
        }
    }
}
