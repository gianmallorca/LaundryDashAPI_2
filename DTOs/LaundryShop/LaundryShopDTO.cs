namespace LaundryDashAPI_2.DTOs.LaundryShop
{
    public class LaundryShopDTO
    {
        public Guid LaundryShopId { get; set; }
        public string LaundryShopName { get; set; }
        public string City { get; set; }
        public string Barangay { get; set; }
        public string BrgyStreet { get; set; }

        public string Address
        {
            get
            {
                return $"{City},{Barangay}, {BrgyStreet}";
            }
        }

        public string? ContactNum { get; set; }
        public string? TimeOpen { get; set; }
        public string? TimeClose { get; set; }
        public bool? Monday { get; set; }
        public bool? Tuesday { get; set; }
        public bool? Wednesday { get; set; }
        public bool? Thursday { get; set; }
        public bool? Friday { get; set; }
        public bool? Saturday { get; set; }
        public bool? Sunday { get; set; }

    }
}
