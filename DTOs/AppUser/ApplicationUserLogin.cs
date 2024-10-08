﻿using DataAnnotationsExtensions;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.DTOs.AppUser
{
    public class ApplicationUserLogin
    {
        [Required]
        [Email]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }

    }
}
