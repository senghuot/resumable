using System;
using System.ComponentModel.DataAnnotations;

namespace ProductsApp.Models {
    public class Update {
        [Required]
        [MaxLength(140)]
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }
}