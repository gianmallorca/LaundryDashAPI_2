using Microsoft.AspNetCore.Identity;

namespace LaundryDashAPI_2.Entities
{
    public class ApplicationUser:IdentityUser
    {
        public Guid Id { get; set; }
        public string FirstName {  get; set; }
        public string LastName { get; set; }
        public bool IsApproved { get; set; }
        public string? UserType { get; set; }
    }
}
