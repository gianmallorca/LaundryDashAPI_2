namespace LaundryDashAPI_2.Entities
{
    public class LaundryServiceLog
    {
        public Guid LaundryServiceLogId { get; set; }

        // LaundryShop
        public LaundryShop LaundryShop { get; set; }
        public Guid LaundryShopId { get; set; }

        // Service IDs
        public List<Guid> ServiceIds { get; set; } = new List<Guid>();

        // New property to store the name of the laundry shop
        public string LaundryShopName { get; set; }

        // New property to store the name of the service
        public string ServiceName { get; set; }

        public decimal? Price { get; set; }

        public string? AddedById { get; set; }
    }
}
