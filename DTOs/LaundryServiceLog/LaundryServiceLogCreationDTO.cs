namespace LaundryDashAPI_2.DTOs.LaundryServiceLog
{
    public class LaundryServiceLogCreationDTO
    {
        public Guid LaundryServiceLogId { get; set; }
        public Guid LaundryShopId { get; set; }
        public List<Guid> ServiceIds { get; set; }
        public decimal? Price { get; set; }

    }
}
