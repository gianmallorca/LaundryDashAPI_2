namespace LaundryDashAPI_2.DTOs
{
    public class LaundryServiceLogCreationDTO
    {
        public Guid LaundryShopId { get; set; }
        public List<Guid> ?ServiceIds { get; set; }
    }
}
