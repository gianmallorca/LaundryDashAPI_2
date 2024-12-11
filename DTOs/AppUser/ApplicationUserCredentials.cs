using DataAnnotationsExtensions;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.DTOs.AppUser
{
    public class ApplicationUserCredentials
    {
        public string? Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string? UserType { get; set; }
        [Required]
        public DateTime? Birthday { get; set; }

        public int? Age
        {
            get
            {
                if (Birthday.HasValue)
                {
                    var today = DateTime.Today;
                    int age = today.Year - Birthday.Value.Year;
                    if (Birthday.Value.Date > today.AddYears(-age)) age--;
                    return age;
                }
                return null;
            }
        }

        [Required]
        public string? Gender { get; set; }
        public string? City { get; set; }
        [Required]
        public string? Barangay { get; set; }
        [Required]
        public string? BrgyStreet { get; set; }

        // Address formatting
        public string Address => $"{City}, {Barangay}, {BrgyStreet}";
        public string? UserAddress { get; set; }

        //laundry shop account prop
        public string? TaxIdentificationNumber { get; set; }
        public string? BusinessPermitNumber { get; set; }

        //rider account prop
        public string? VehicleType { get; set; }
        public decimal? VehicleCapacity { get; set; }
        public string? DriversLicenseNumber { get; set; }

        [Required]
        [Email]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string ConfirmPassword { get; set; }
    }
}
