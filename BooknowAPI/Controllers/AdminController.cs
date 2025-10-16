//using System;
//using System.Linq;
//using System.Web.Http;
//using BooknowAPI.Models;

//namespace RestAdvBook.Controllers
//{
//    [RoutePrefix("api/admin")]
//    public class AdminController : ApiController
//    {
//        private newRestdbEntities4 db = new newRestdbEntities4();

//        // ✅ 1. Get All Chefs by Admin
//        [HttpGet]
//        [Route("GetAllChef/{adminUserId}")]
//        public IHttpActionResult GetAllChef(int adminUserId)
//        {
//            var admin = db.Admins.FirstOrDefault(a => a.UserId == adminUserId);
//            if (admin == null)
//                return BadRequest("Admin not found.");

//            var restaurantId = admin.RestaurantId;
//            var chefs = db.Users
//                .Where(u => u.Role == "Chef" && u.RestaurantId == restaurantId)
//                .Select(u => new
//                {
//                    u.UserId,
//                    u.Name,
//                    u.Email,
//                    u.Speciality
//                })
//                .ToList();

//            if (!chefs.Any())
//                return Ok("No chefs found for this restaurant.");

//            return Ok(chefs);
//        }

//        // ✅ 2. Create a New Chef
//        [HttpPost]
//        [Route("CreateChef")]
//        public IHttpActionResult CreateChef([FromBody] User newChef)
//        {
//            if (newChef == null)
//                return BadRequest("Invalid chef data.");

//            var admin = db.Admins.FirstOrDefault(a => a.UserId == newChef.CreatedByAdminId);
//            if (admin == null)
//                return BadRequest("Admin not found.");

//            newChef.RestaurantId = admin.RestaurantId;
//            newChef.Role = "Chef";
//            newChef.CoinBalance = 0;

//            db.Users.Add(newChef);
//            db.SaveChanges();

//            return Ok("Chef created successfully.");
//        }

//        // ✅ 3. Get Tables by Admin
//        [HttpGet]
//        [Route("GetTablesByAdmin/{adminUserId}")]
//        public IHttpActionResult GetTablesByAdmin(int adminUserId)
//        {
//            var admin = db.Admins.FirstOrDefault(a => a.UserId == adminUserId);
//            if (admin == null)
//                return BadRequest("Admin not found.");

//            var restaurantId = admin.RestaurantId;
//            var tables = db.Tables.Where(t => t.RestaurantId == restaurantId).ToList();

//            if (!tables.Any())
//                return Ok("No tables found for this restaurant.");

//            return Ok(tables);
//        }

//        // ✅ 4. Add a New Table by Admin
//        [HttpPost]
//        [Route("AddTable/{adminUserId}")]
//        public IHttpActionResult AddTable(int adminUserId, [FromBody] Table newTable)
//        {
//            if (newTable == null)
//                return BadRequest("Invalid table data.");

//            var admin = db.Admins.FirstOrDefault(a => a.UserId == adminUserId);
//            if (admin == null)
//                return BadRequest("Admin not found.");

//            newTable.RestaurantId = admin.RestaurantId;
//            newTable.Status = "Available";

//            db.Tables.Add(newTable);
//            db.SaveChanges();

//            return Ok("Table added successfully.");
//        }

//        // ✅ 5. Update Existing Table
//        [HttpPut]
//        [Route("UpdateTable/{tableId}")]
//        public IHttpActionResult UpdateTable(int tableId, [FromBody] Table updatedTable)
//        {
//            var table = db.Tables.FirstOrDefault(t => t.TableId == tableId);
//            if (table == null)
//                return NotFound();

//            table.Name = updatedTable.Name;
//            table.Floor = updatedTable.Floor;
//            table.Price = updatedTable.Price;
//            table.Location = updatedTable.Location;
//            table.Capacity = updatedTable.Capacity;
//            table.Status = updatedTable.Status;

//            db.SaveChanges();
//            return Ok("Table updated successfully.");
//        }

//        // ✅ 6. Delete Table
//        [HttpDelete]
//        [Route("DeleteTable/{tableId}")]
//        public IHttpActionResult DeleteTable(int tableId)
//        {
//            var table = db.Tables.FirstOrDefault(t => t.TableId == tableId);
//            if (table == null)
//                return NotFound();

//            db.Tables.Remove(table);
//            db.SaveChanges();

//            return Ok("Table deleted successfully.");
//        }

//        protected override void Dispose(bool disposing)
//        {
//            if (disposing)
//                db.Dispose();
//            base.Dispose(disposing);
//        }
//    }
//}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using BooknowAPI.Models;
using Newtonsoft.Json.Linq;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/admin")]
    public class AdminController : ApiController
    {
        private newRestdbEntities7 db = new newRestdbEntities7();

        // 🔹 Create a new Chef (with multiple dish specialities)
        [HttpPost]
        [Route("CreateChef")]
        public IHttpActionResult CreateChef([FromBody] JObject data)
        {
            if (data == null)
                return BadRequest("Invalid data.");

            var userJson = data["user"];
            var adminUserId = data["adminUserId"]?.ToObject<int>() ?? 0;
            var dishNames = data["dishNames"]?.ToObject<List<string>>();

            if (userJson == null || adminUserId == 0 || dishNames == null || !dishNames.Any())
                return BadRequest("Missing required fields.");

            var user = userJson.ToObject<User>();

            if (user.Role != "Chef")
                return BadRequest("Only 'Chef' role is allowed.");

            var admin = db.Admins.FirstOrDefault(a => a.UserId == adminUserId);
            if (admin == null)
                return BadRequest("Admin not found.");

            int restaurantId = (int)admin.RestaurantId;

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

            // Add specialities
            var addedSpecialities = new List<object>();
            foreach (var dishName in dishNames)
            {
                var dish = db.Dishes.FirstOrDefault(d => d.Name.ToLower() == dishName.ToLower());
                if (dish != null)
                {
                    var speciality = new ChefDishSpeciality
                    {
                        UserId = user.UserId,
                        DishId = dish.DishId
                    };
                    db.ChefDishSpecialities.Add(speciality);

                    addedSpecialities.Add(new { DishName = dish.Name });
                }
                else
                {
                    return BadRequest($"Dish '{dishName}' not found in the menu.");
                }
            }

            db.SaveChanges();

            return Ok(new
            {
                Message = "Chef created with specialities.",
                ChefId = user.UserId,
                RestaurantId = restaurantId,
                Specialities = addedSpecialities
            });
        }

        // 🔹 Get Tables by Admin UserId
        [HttpGet]
        [Route("GetTablesByAdmin/{adminUserId}")]
        public IHttpActionResult GetTablesByAdmin(int adminUserId)
        {
            var admin = db.Admins.FirstOrDefault(a => a.UserId == adminUserId);
            if (admin == null)
                return BadRequest("Admin not found.");

            int restaurantId = (int)admin.RestaurantId;

            var tables = db.Tables
                .Where(t => t.RestaurantId == restaurantId)
                .Select(t => new
                {
                    t.TableId,
                    t.Name,
                    t.Floor,
                    t.Status,
                    t.Price,
                    t.Location,
                    t.Capacity
                })
                .ToList();

            if (!tables.Any())
                return NotFound();

            return Ok(tables);
        }
        [HttpGet]
        [Route("GetAllChef/{adminUserId}")]
        public IHttpActionResult GetAllChef(int adminUserId)
        {
            // ✅ 1. Find the admin
            var admin = db.Admins.FirstOrDefault(a => a.UserId == adminUserId);
            if (admin == null)
                return BadRequest("Admin not found.");

            var restaurantId = admin.RestaurantId;
            if (restaurantId == null)
                return BadRequest("This admin is not linked to any restaurant.");

            // ✅ 2. Get all chefs belonging to that restaurant
            var chefs = db.Chefs
                .Where(c => c.RestaurantId == restaurantId)
                .Select(c => new
                {
                    c.UserId,
                    Name = c.User.Name,
                    Email = c.User.Email,

                    // ✅ 3. Fetch all specialities for this chef (Dish names)
                    Specialities = db.ChefDishSpecialities
                        .Where(cs => cs.UserId == c.UserId)
                        .Select(cs => cs.Dish.Name)
                        .ToList()
                })
                .ToList();

            if (!chefs.Any())
                return Ok("No chefs found for this restaurant.");

            return Ok(chefs);
        }
        [HttpPost]
        [Route("CreateChef/{adminUserId}")]
        public IHttpActionResult CreateChef(int adminUserId, [FromBody] User newChef)
        {
            if (newChef == null)
                return BadRequest("Invalid chef data.");

            var admin = db.Admins.FirstOrDefault(a => a.UserId == adminUserId);
            if (admin == null)
                return BadRequest("Admin not found.");

            if (admin.RestaurantId == null)
                return BadRequest("Admin is not linked to any restaurant.");

            // 🔹 Create the user (chef)
            newChef.Role = "Chef";
            db.Users.Add(newChef);
            db.SaveChanges();

            // 🔹 Link to restaurant
            var chef = new Chef
            {
                UserId = newChef.UserId,
                RestaurantId = admin.RestaurantId
            };
            db.Chefs.Add(chef);
            db.SaveChanges();

            // 🔹 Handle specialities (sent from Swift as comma-separated dish names)
            if (!string.IsNullOrEmpty(newChef.Speciality))
            {
                var specialityNames = newChef.Speciality.Split(',')
                    .Select(s => s.Trim())
                    .ToList();

                foreach (var dishName in specialityNames)
                {
                    var dish = db.Dishes.FirstOrDefault(d => d.Name.ToLower() == dishName.ToLower());
                    if (dish != null)
                    {
                        var chefSpec = new ChefDishSpeciality
                        {
                            UserId = newChef.UserId,
                            DishId = dish.DishId
                        };
                        db.ChefDishSpecialities.Add(chefSpec);
                    }
                }

                db.SaveChanges();
            }

            return Ok(new
            {
                Message = "Chef created successfully.",
                newChef.UserId,
                newChef.Name,
                newChef.Email,
                Specialities = newChef.Speciality
            });
        }


        // 🔹 Add Table for Admin’s Restaurant
        [HttpPost]
        [Route("AddTable/{adminUserId}")]
        public IHttpActionResult AddTable(int adminUserId, [FromBody] Table newTable)
        {
            if (newTable == null)
                return BadRequest("Invalid table data.");

            var admin = db.Admins.FirstOrDefault(a => a.UserId == adminUserId);
            if (admin == null)
                return BadRequest("Admin not found or unauthorized.");

            newTable.RestaurantId = admin.RestaurantId;

            if (string.IsNullOrEmpty(newTable.Status))
                newTable.Status = "Available";

            db.Tables.Add(newTable);
            db.SaveChanges();

            return Ok(new
            {
                Message = "✅ Table added successfully!",
                TableId = newTable.TableId,
                RestaurantId = newTable.RestaurantId
            });
        }

        // 🔹 Get All Tables (all restaurants)
        [HttpGet]
        [Route("GetAllTables")]
        public IHttpActionResult GetAllTables()
        {
            var tables = db.Tables.ToList();
            return Ok(tables);
        }

        // 🔹 Get Tables by Restaurant
        [HttpGet]
        [Route("GetTablesByRestaurant/{restaurantId}")]
        public IHttpActionResult GetTablesByRestaurant(int restaurantId)
        {
            var tables = db.Tables.Where(t => t.RestaurantId == restaurantId).ToList();

            if (!tables.Any())
                return BadRequest("No tables found for this restaurant.");

            return Ok(tables);
        }
    }
}

