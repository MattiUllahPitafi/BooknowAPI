using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BooknowAPI.Models;

namespace BookNowApI.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Property
        public virtual User User { get; set; }
    }
}
