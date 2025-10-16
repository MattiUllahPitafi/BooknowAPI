using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BooknowAPI.Models
{
    public partial class Booking
    {
        // ✅ Temporary field for DedicationNote
        // Used to pass dedication text from frontend but NOT stored in the Booking table
        [NotMapped]
        public string DedicationNote { get; set; }
        //public DateTime RequestedAt { get; internal set; }
    }
}
