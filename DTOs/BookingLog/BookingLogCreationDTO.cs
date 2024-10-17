namespace LaundryDashAPI_2.DTOs.BookingLog
{
    public class BookingLogCreationDTO
    {
        public Guid LaundryServiceLogId { get; set; }
        public string PickupAddress { get; set; }
        public string DeliveryAddress { get; set; }
    }
}
