namespace LaundryDashAPI_2.Entities
{
    public class LaundryServiceLog
    {
        public Guid LaundryServiceLogId { get; set; }

        //LaundryShop
        public LaundryShop LaundryShop { get; set; }
        public Guid LaundryShopId { get; set; }

        //Service
        public Service Service { get; set; }
        public Guid ServiceId { get; set; }
    }
}
