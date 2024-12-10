namespace LaundryDashAPI_2.DTOs.AppUser
{
    public class UserTypeDTO
    {
        public string UserType { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string City { get; set; }
        public string Barangay { get; set; }
        public string BrgyStreet { get; set; }
        public string UserAddress => $"{City}, {Barangay}, {BrgyStreet}";
    }
}
