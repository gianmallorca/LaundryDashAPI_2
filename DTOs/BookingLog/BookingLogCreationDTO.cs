namespace LaundryDashAPI_2.DTOs.BookingLog
{
    public class BookingLogCreationDTO
    {
        public Guid LaundryServiceLogId { get; set; } // ID of the associated laundry service log
        public string PickupAddress { get; set; } // Pickup address for the booking
        public string DeliveryAddress { get; set; } // Delivery address for the booking
        public string? Note { get; set; } // Optional note from the client
        public decimal? TotalPrice { get; set; } // Total price of the booking
        public decimal? Weight { get; set; } // Optional weight of the laundry
        public string? PaymentMethod { get; set; }
    }
}
