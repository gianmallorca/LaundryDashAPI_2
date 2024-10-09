namespace LaundryDashAPI_2.DTOs.LaundryServiceLog
{
    public class LaundryServiceLogDTO
    {
        public Guid LaundryServiceLogId { get; set; }
        public Guid LaundryShopId { get; set; }
        public Guid ServiceId { get; set; }
        public decimal? Price { get; set; }
    }
}
