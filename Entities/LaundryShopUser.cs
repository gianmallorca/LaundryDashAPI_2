using Microsoft.AspNetCore.Identity;

namespace LaundryDashAPI_2.Entities
{
    public class LaundryShopUser:IdentityUser
    {
        public Guid Id { get; set; }
        public string FirstName {  get; set; }
        public string LastName { get; set; }
        public bool IsApproved { get; set; } = false;
    }
}
