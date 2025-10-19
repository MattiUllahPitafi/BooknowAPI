
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using BooknowAPI.Models;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Web;
using System.Net.Http;
using System.Net;
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
        [HttpPost]
        [Route("CreateDish")]
        public IHttpActionResult CreateDish()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest == null || httpRequest.Form.Count == 0)
                    return BadRequest("No form data received.");

                // ------------------------------
                // 1️⃣ Parse and validate fields
                // ------------------------------
                if (!int.TryParse(httpRequest["UserId"], out int userId))
                    return BadRequest("Invalid or missing UserId.");

                string name = (httpRequest["Name"] ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest("Dish name is required.");

                if (!decimal.TryParse(httpRequest["Price"], out decimal price))
                    return BadRequest("Invalid price value.");

                int prepTime = 0;
                int.TryParse(httpRequest["PrepTimeMinutes"], out prepTime);

                string menuCategoryName = (httpRequest["MenuCategoryName"] ?? "").Trim();
                if (string.IsNullOrWhiteSpace(menuCategoryName))
                    return BadRequest("Menu category name is required.");

                if (!decimal.TryParse(httpRequest["BaseQuantity"], out decimal baseQuantity))
                    baseQuantity = 1;

                string unit = string.IsNullOrWhiteSpace(httpRequest["Unit"]) ? "plate" : httpRequest["Unit"];
                string ingredientsJson = httpRequest["Ingredients"]; // optional JSON string

                // ------------------------------
                // 2️⃣ Validate Admin & Restaurant
                // ------------------------------
                var admin = db.Admins.FirstOrDefault(a => a.UserId == userId);
                if (admin == null)
                    return BadRequest("Invalid Admin User ID.");

                if (admin.RestaurantId == null || admin.RestaurantId == 0)
                    return BadRequest("Admin is not linked to any restaurant.");

                int restaurantId = admin.RestaurantId.Value;

                // ------------------------------
                // 3️⃣ Handle optional image upload
                // ------------------------------
                string dishImageUrl = null;
                if (httpRequest.Files.Count > 0)
                {
                    var postedFile = httpRequest.Files["DishImage"] ?? httpRequest.Files[0];
                    if (postedFile != null && postedFile.ContentLength > 0)
                    {
                        string folderPath = @"C:\images";
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(postedFile.FileName)}";
                        string fullPath = Path.Combine(folderPath, fileName);
                        postedFile.SaveAs(fullPath);

                        dishImageUrl = $"images/{fileName}";
                    }
                }

                // ------------------------------
                // 4️⃣ Find or create MenuCategory
                // ------------------------------
                var category = db.MenuCategories.FirstOrDefault(c => c.Name == menuCategoryName);

                if (category == null)
                {
                    category = new MenuCategory
                    {
                        Name = menuCategoryName
                    };
                    db.MenuCategories.Add(category);
                    db.SaveChanges();
                }

                // ------------------------------
                // 5️⃣ Create Dish
                // ------------------------------
                var dish = new Dish
                {
                    Name = name,
                    Price = price,
                    PrepTimeMinutes = prepTime,
                    RestaurantId = restaurantId,
                    MenuCategoryId = category.MenuCategoryId,
                    BaseQuantity = baseQuantity,
                    Unit = unit,
                    DishImageUrl = dishImageUrl
                };

                db.Dishes.Add(dish);
                db.SaveChanges();

                // ------------------------------
                // 6️⃣ Add ingredients if provided
                // ------------------------------
                if (!string.IsNullOrEmpty(ingredientsJson))
                {
                    try
                    {
                        JArray ingredientsArray = JArray.Parse(ingredientsJson);

                        var recipesToAdd = new List<DishRecipe>();

                        foreach (var ing in ingredientsArray)
                        {
                            string ingName = (string)ing["Name"];
                            if (string.IsNullOrWhiteSpace(ingName))
                                return BadRequest("Ingredient Name is missing.");

                            if (!decimal.TryParse(ing["QuantityRequired"]?.ToString(), out decimal qty))
                                return BadRequest($"Invalid quantity for ingredient '{ingName}'.");

                            var ingredient = db.Ingredients.FirstOrDefault(i =>
                                i.Name == ingName && i.restaurant_id == restaurantId);

                            if (ingredient == null)
                                return BadRequest($"Ingredient '{ingName}' not found in restaurant ID {restaurantId}.");

                            recipesToAdd.Add(new DishRecipe
                            {
                                DishId = dish.DishId,
                                IngredientId = ingredient.IngredientId,
                                QuantityRequired = qty
                            });
                        }

                        db.DishRecipes.AddRange(recipesToAdd);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return Content(System.Net.HttpStatusCode.InternalServerError, new
                        {
                            message = "❌ Error creating dish",
                            error = ex.Message,
                            inner = ex.InnerException?.Message
                        });
                    }

                }

                // ------------------------------
                // 7️⃣ Return success response
                // ------------------------------
                return Ok(new
                {
                    message = "✅ Dish created successfully",
                    DishId = dish.DishId,
                    RestaurantId = restaurantId,
                    ImagePath = dishImageUrl
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new
                {
                    message = "❌ Error creating dish",
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }

        }


        //[HttpPost]
        //[Route("create")]
        //public IHttpActionResult CreateDish()
        //{
        //    try
        //    {
        //        var httpRequest = HttpContext.Current.Request;

        //        // ✅ Read fields from FormData
        //        int userId = int.Parse(httpRequest["UserId"]);
        //        string name = httpRequest["Name"];
        //        decimal price = Convert.ToDecimal(httpRequest["Price"]);
        //        int prepTime = Convert.ToInt32(httpRequest["PrepTimeMinutes"]);
        //        string menuCategoryName = httpRequest["MenuCategoryName"];
        //        decimal baseQuantity = Convert.ToDecimal(httpRequest["BaseQuantity"]);
        //        string unit = httpRequest["Unit"];
        //        string ingredientsJson = httpRequest["Ingredients"]; // JSON array of ingredient name + quantity

        //        // ✅ Validate Admin
        //        var admin = db.Admins.FirstOrDefault(a => a.UserId == userId);
        //        if (admin == null)
        //            return BadRequest("Invalid Admin User ID.");

        //        int restaurantId = admin.RestaurantId ?? 0;
        //        if (restaurantId == 0)
        //            return BadRequest("Admin is not linked to any restaurant.");

        //        // ✅ Handle optional image upload
        //        string dishImageUrl = null;
        //        if (httpRequest.Files.Count > 0)
        //        {
        //            var postedFile = httpRequest.Files[0];
        //            if (postedFile != null && postedFile.ContentLength > 0)
        //            {
        //                string fileName = Path.GetFileName(postedFile.FileName);
        //                string filePath = HttpContext.Current.Server.MapPath("~/Uploads/" + fileName);
        //                postedFile.SaveAs(filePath);
        //                dishImageUrl = "/Uploads/" + fileName;
        //            }
        //        }

        //        // ✅ Validate Category
        //        var category = db.MenuCategories.FirstOrDefault(c => c.Name == menuCategoryName);
        //        if (category == null)
        //            return BadRequest("Invalid menu category name.");

        //        // ✅ Create Dish
        //        var dish = new Dish
        //        {
        //            Name = name,
        //            Price = price,
        //            PrepTimeMinutes = prepTime,
        //            RestaurantId = restaurantId,
        //            MenuCategoryId = category.MenuCategoryId,
        //            BaseQuantity = baseQuantity,
        //            Unit = unit,
        //            DishImageUrl = dishImageUrl
        //        };

        //        db.Dishes.Add(dish);
        //        db.SaveChanges();

        //        // ✅ Parse ingredients JSON (e.g. [{"Name":"Tomato","QuantityRequired":2},{"Name":"Cheese","QuantityRequired":1}])
        //        if (!string.IsNullOrEmpty(ingredientsJson))
        //        {
        //            var ingredientsList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(ingredientsJson);

        //            foreach (var ing in ingredientsList)
        //            {
        //                string ingName = ing.Name;
        //                decimal qty = ing.QuantityRequired;

        //                var ingredient = db.Ingredients.FirstOrDefault(i => i.Name == ingName && i.restaurant_id == restaurantId);
        //                if (ingredient == null)
        //                    return BadRequest($"Ingredient '{ingName}' not found in this restaurant.");

        //                var recipe = new DishRecipe
        //                {
        //                    DishId = dish.DishId,
        //                    IngredientId = ingredient.IngredientId,
        //                    QuantityRequired = qty
        //                };
        //                db.DishRecipes.Add(recipe);
        //            }
        //            db.SaveChanges();
        //        }

        //        return Ok(new
        //        {
        //            message = "Dish created successfully",
        //            DishId = dish.DishId,
        //            RestaurantId = restaurantId
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}


        // ✅ GET dishes by Admin
        [HttpGet]
        [Route("getByAdmin/{userId}")]
        public IHttpActionResult GetDishesByAdmin(int userId)
        {
            try
            {
                var admin = db.Admins.FirstOrDefault(a => a.UserId == userId);
                if (admin == null)
                    return BadRequest("Invalid Admin User ID.");

                int? restaurantId = admin.RestaurantId;
                if (restaurantId == null)
                    return BadRequest("Admin is not linked with any restaurant.");

                var dishes = db.Dishes
                    .Where(d => d.RestaurantId == restaurantId)
                    .Select(d => new
                    {
                        d.DishId,
                        d.Name,
                        d.Price,
                        d.PrepTimeMinutes,
                        d.Unit,
                        d.BaseQuantity,
                        d.DishImageUrl,
                        MenuCategory = d.MenuCategory.Name,
                        Ingredients = d.DishRecipes.Select(dr => new
                        {
                            dr.Ingredient.IngredientId,
                            dr.Ingredient.Name,
                            dr.QuantityRequired,
                            dr.Ingredient.Unit
                        }).ToList()
                    })
                    .ToList();

                if (!dishes.Any())
                    return Ok(new { message = "No dishes found for this restaurant." });

                return Ok(new
                {
                    RestaurantId = restaurantId,
                    TotalDishes = dishes.Count,
                    Dishes = dishes
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ✅ UPDATE dish by Admin
        [HttpPut]
        [Route("update")]
        public IHttpActionResult UpdateDish([FromBody] dynamic data)
        {
            try
            {
                int userId = data.UserId;
                int dishId = data.DishId;
                string name = data.Name;
                decimal price = data.Price;
                int prepTime = data.PrepTimeMinutes;
                string menuCategoryName = data.MenuCategoryName;
                decimal baseQuantity = data.BaseQuantity;
                string unit = data.Unit;
                string dishImageUrl = data.DishImageUrl;

                var admin = db.Admins.FirstOrDefault(a => a.UserId == userId);
                if (admin == null)
                    return BadRequest("Invalid Admin User ID.");

                int? restaurantId = admin.RestaurantId;
                if (restaurantId == null)
                    return BadRequest("Admin is not linked with any restaurant.");

                var dish = db.Dishes.FirstOrDefault(d => d.DishId == dishId && d.RestaurantId == restaurantId);
                if (dish == null)
                    return BadRequest("Dish not found or does not belong to your restaurant.");

                dish.Name = name;
                dish.Price = price;
                dish.PrepTimeMinutes = prepTime;
                dish.BaseQuantity = baseQuantity;
                dish.Unit = unit;
                dish.DishImageUrl = dishImageUrl;

                if (!string.IsNullOrEmpty(menuCategoryName))
                {
                    var category = db.MenuCategories.FirstOrDefault(c => c.Name == menuCategoryName);
                    if (category == null)
                        return BadRequest("Invalid menu category name.");
                    dish.MenuCategoryId = category.MenuCategoryId;
                }

                var ingredients = new List<dynamic>();
                foreach (var ing in data.Ingredients)
                    ingredients.Add(ing);

                var existingRecipes = db.DishRecipes.Where(r => r.DishId == dishId).ToList();
                db.DishRecipes.RemoveRange(existingRecipes);

                foreach (var ing in ingredients)
                {
                    string ingName = ing.Name;
                    decimal qty = ing.QuantityRequired;

                    var ingredient = db.Ingredients.FirstOrDefault(i => i.Name == ingName && i.restaurant_id == restaurantId);
                    if (ingredient == null)
                        return BadRequest($"Ingredient '{ingName}' not found in this restaurant.");

                    var recipe = new DishRecipe
                    {
                        DishId = dish.DishId,
                        IngredientId = ingredient.IngredientId,
                        QuantityRequired = qty
                    };

                    db.DishRecipes.Add(recipe);
                }

                db.SaveChanges();
                return Ok(new { message = "Dish updated successfully", DishId = dish.DishId });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ✅ DELETE dish by Admin
        [HttpDelete]
        [Route("delete/{userId}/{dishId}")]
        public IHttpActionResult DeleteDish(int userId, int dishId)
        {
            try
            {
                var admin = db.Admins.FirstOrDefault(a => a.UserId == userId);
                if (admin == null)
                    return BadRequest("Invalid Admin User ID.");

                int? restaurantId = admin.RestaurantId;
                if (restaurantId == null)
                    return BadRequest("Admin is not linked with any restaurant.");

                var dish = db.Dishes.FirstOrDefault(d => d.DishId == dishId && d.RestaurantId == restaurantId);
                if (dish == null)
                    return BadRequest("Dish not found or does not belong to your restaurant.");

                var recipes = db.DishRecipes.Where(r => r.DishId == dishId).ToList();
                db.DishRecipes.RemoveRange(recipes);

                db.Dishes.Remove(dish);
                db.SaveChanges();

                return Ok(new { message = "Dish deleted successfully", DishId = dishId });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}

    
