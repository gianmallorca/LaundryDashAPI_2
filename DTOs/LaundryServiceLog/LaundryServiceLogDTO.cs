namespace LaundryDashAPI_2.DTOs.LaundryServiceLog
{
    public class LaundryServiceLogDTO
    {
        public Guid LaundryServiceLogId { get; set; }
        public Guid LaundryShopId { get; set; }
        public string LaundryShopName { get; set; } // Include laundry shop name
        public List<Guid> ServiceIds { get; set; } // Keep service IDs
        public string ServiceName { get; set; } // Include service name
        public decimal? Price { get; set; }
    }
}