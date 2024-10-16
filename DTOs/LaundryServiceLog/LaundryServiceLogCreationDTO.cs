namespace LaundryDashAPI_2.DTOs.LaundryServiceLog
{
    public class LaundryServiceLogCreationDTO
    {
        public Guid LaundryShopId { get; set; }
        public List<Guid> ServiceIds { get; set; } = new List<Guid>();
        public decimal? Price { get; set; }

        // New properties to hold names
        public string LaundryShopName { get; set; }
        public List<string> ServiceNames { get; set; } = new List<string>();
    }
}
