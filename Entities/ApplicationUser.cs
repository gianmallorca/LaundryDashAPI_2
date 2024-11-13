using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;

namespace LaundryDashAPI_2.Entities
{
    public class ApplicationUser : IdentityUser
    {
        //     public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? Birthday { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string? Barangay { get; set; }
        public string? BrgyStreet { get; set; }
        public bool IsApproved { get; set; }
        public string? UserType { get; set; }

        //laundry shop account prop
        public string? TaxIdentificationNumber { get; set; }
        public string? BusinessPermitNumber { get; set; }

        //rider account prop
        public string? VehicleType { get; set; }
        public decimal? VehicleCapacity {get;set;}
        public string? DriversLicenseNumber { get; set; }


    }
}
