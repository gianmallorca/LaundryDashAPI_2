using DataAnnotationsExtensions;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.DTOs.AppUser
{
    public class ApplicationUserCredentials
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public DateTime? Birthday { get; set; }
        public int? Age { get; set; }
        [Required]
        public string? Gender { get; set; }
        public string? City { get; set; }
        [Required]
        public string? Barangay { get; set; }
        [Required]
        public string? BrgyStreet { get; set; }

        //will format the address to this
        public string Address
        {
            get
            {
                return $"{City},{Barangay}, {BrgyStreet}";
            }
        }
        [Required]
        [Email]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }



    }
}
