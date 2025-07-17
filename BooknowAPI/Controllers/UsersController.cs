using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using BooknowAPI.Models;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/Users")]
    public class UsersController : ApiController
    {
        private newRestdbEntities db = new newRestdbEntities();

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
            var user = db.Users.Where(u => u.UserId == id).Select(u => new
            {
                u.UserId,
                u.Name,
                u.Email,
                u.Role
            }).FirstOrDefault();

            if (user == null)
                return NotFound();

            return Ok(user);
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
        [Route("CreateStaff")]
        public IHttpActionResult CreateStaff([FromBody] User user)
        {
            if (user == null || (user.Role != "Chef" && user.Role != "Waiter"))
                return BadRequest("Only 'Chef' or 'Waiter' can be created.");

            if (string.IsNullOrEmpty(user.PasswordHash))
                return BadRequest("Password is required.");

            db.Users.Add(user);
            db.SaveChanges();

            return Ok(new { Message = "Staff created successfully", user.UserId });
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
