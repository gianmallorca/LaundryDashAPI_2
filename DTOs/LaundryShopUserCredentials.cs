﻿using DataAnnotationsExtensions;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.DTOs
{
    public class LaundryShopUserCredentials
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        [Email]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public bool IsApproved { get; set; }=false;

    }
}
