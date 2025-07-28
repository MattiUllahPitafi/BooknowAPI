using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;
using BooknowAPI.Models;

namespace RestWAdvBook.Controllers
{
    [RoutePrefix("api/OrderAndStock")]
    public class OrderAndStockController : ApiController
    {
        private newRestdbEntities2 db = new newRestdbEntities2();

        // =================== INGREDIENTS ===================

        [HttpGet]
        [Route("ingredients/{restaurantId:int}")]
        public IHttpActionResult GetIngredients(int restaurantId, string name = null)
        {
            var query = db.Ingredients.Where(i => i.restaurant_id == restaurantId);
            if (!string.IsNullOrEmpty(name))
                query = query.Where(i => i.Name.Contains(name));

            return Ok(query.ToList());
        }

        [HttpPost]
        [Route("ingredient/add")]
        public IHttpActionResult AddIngredient(Ingredient ingredient)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            db.Ingredients.Add(ingredient);
            db.SaveChanges();
            return Ok("Ingredient added successfully.");
        }

        [HttpPut]
        [Route("ingredient/update/{id:int}")]
        public IHttpActionResult UpdateIngredient(int id, Ingredient updatedIngredient)
        {
            var existing = db.Ingredients.Find(id);
            if (existing == null)
                return NotFound();

            existing.Name = updatedIngredient.Name;
            existing.QuantityInStock = updatedIngredient.QuantityInStock;
            existing.Unit = updatedIngredient.Unit;
            existing.restaurant_id = updatedIngredient.restaurant_id;

            db.SaveChanges();
            return Ok("Ingredient updated.");
        }

        [HttpDelete]
        [Route("ingredient/delete/{id:int}")]
        public IHttpActionResult DeleteIngredient(int id)
        {
            var ingredient = db.Ingredients.Find(id);
            if (ingredient == null)
                return NotFound();

            db.Ingredients.Remove(ingredient);
            db.SaveChanges();
            return Ok("Deleted.");
        }

        // =================== DISH RECIPES ===================

        [HttpGet]
        [Route("recipes/{restaurantId:int}")]
        public IHttpActionResult GetRecipes(int restaurantId, int? dishId = null)
        {
            var query = db.DishRecipes.Include(d => d.Ingredient).Where(r => r.Ingredient.restaurant_id == restaurantId);
            if (dishId != null)
                query = query.Where(r => r.DishId == dishId);

            return Ok(query.ToList());
        }

        [HttpPost]
        [Route("recipe/add")]
        public IHttpActionResult AddRecipe(DishRecipe recipe)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            db.DishRecipes.Add(recipe);
            db.SaveChanges();
            return Ok("Recipe added.");
        }

        [HttpPut]
        [Route("recipe/update/{id:int}")]
        public IHttpActionResult UpdateRecipe(int id, DishRecipe updated)
        {
            var recipe = db.DishRecipes.Find(id);
            if (recipe == null)
                return NotFound();

            recipe.DishId = updated.DishId;
            recipe.IngredientId = updated.IngredientId;
            recipe.QuantityRequired = updated.QuantityRequired;
            db.SaveChanges();
            return Ok("Updated.");
        }

        [HttpDelete]
        [Route("recipe/delete/{id:int}")]
        public IHttpActionResult DeleteRecipe(int id)
        {
            var recipe = db.DishRecipes.Find(id);
            if (recipe == null)
                return NotFound();

            db.DishRecipes.Remove(recipe);
            db.SaveChanges();
            return Ok("Deleted.");
        }

        // =================== STOCK CONSUMPTION ===================
        [HttpPost]
        [Route("consume/{orderId:int}/by-chef/{chefId:int}")]
        public IHttpActionResult MarkOrderInProgress(int orderId, int chefId)
        {
            var order = db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.OrderId == orderId);
            if (order == null)
                return NotFound();

            foreach (var item in order.OrderItems)
            {
                var dish = db.Dishes.FirstOrDefault(m => m.DishId == item.DishId);
                if (dish == null)
                    return BadRequest($"Dish not found for DishId {item.DishId}");

                int restaurantId = (int)dish.RestaurantId;

                var recipes = db.DishRecipes.Where(r => r.DishId == item.DishId);
                foreach (var recipe in recipes)
                {
                    var ingredient = db.Ingredients.FirstOrDefault(i => i.IngredientId == recipe.IngredientId);
                    if (ingredient == null || ingredient.QuantityInStock < recipe.QuantityRequired * item.Quantity)
                        return BadRequest($"Not enough stock for ingredient ID {recipe.IngredientId}");

                    // Deduct stock
                    ingredient.QuantityInStock -= recipe.QuantityRequired * item.Quantity;

                    // Log consumption
                    var log = new StockConsumption
                    {
                        UserId = chefId,
                        IngredientId = recipe.IngredientId,
                        QuantityUsed = recipe.QuantityRequired * item.Quantity,
                        UsedAt = DateTime.Now,
                        order_id = orderId,
                        restaurant_id = restaurantId
                    };
                    db.StockConsumptions.Add(log);
                }
            }

            db.SaveChanges();
            return Ok("Order marked as in progress and ingredients consumed.");
        }

        

        [HttpGet]
        [Route("consumptions/{restaurantId:int}")]
        public IHttpActionResult GetConsumptions(int restaurantId, int? chefId = null)
        {
            var query = db.StockConsumptions.Include(s => s.Ingredient)
                                            .Where(s => s.Ingredient.restaurant_id == restaurantId);
            if (chefId != null)
                query = query.Where(s => s.UserId == chefId);

            return Ok(query.ToList());
        }
    }
}
