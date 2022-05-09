﻿using System.ComponentModel.DataAnnotations;

namespace BackRowCommerceApp.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int? AccountNum { get; set; }
        [Required]
        public string? Message { get; set; }
    }
}
