using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;

namespace LaundryDashAPI_2.Entities
{
    public class ApplicationUser:IdentityUser
    {
   //     public Guid Id { get; set; }
        public string FirstName {  get; set; }
        public string LastName { get; set; }
        public DateTime? Birthday { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string? Barangay { get; set; } 
        public string? BrgyStreet { get;set; }
        public bool IsApproved { get; set; }
        public string? UserType { get; set; }
    }
}
