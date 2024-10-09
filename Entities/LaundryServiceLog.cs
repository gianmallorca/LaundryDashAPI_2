namespace LaundryDashAPI_2.Entities
{
    public class LaundryServiceLog
    {
        public Guid LaundryServiceLogId { get; set; }

        //LaundryShop
        public LaundryShop LaundryShop { get; set; }
        public Guid LaundryShopId { get; set; }

        //Service
        public List<Guid> ServiceIds { get; set; } = new List<Guid>();


        public string? AddedById { get; set; }
    }
}
