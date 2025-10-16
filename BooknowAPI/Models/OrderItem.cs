namespace BooknowAPI.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class OrderItem
    {
        public int OrderItemId { get; set; }
        public Nullable<int> OrderId { get; set; }
        public Nullable<int> DishId { get; set; }
        public Nullable<int> Quantity { get; set; }
        public decimal QuantityOrdered { get; set; }
        public decimal UnitPrice { get; set; }
        public string SkippedIngredientIds { get; set; }

        public virtual Dish Dish { get; set; }
        public virtual Order Order { get; set; }
        [NotMapped]
        public List<int> SkippedIngredients { get; set; }
    }
}