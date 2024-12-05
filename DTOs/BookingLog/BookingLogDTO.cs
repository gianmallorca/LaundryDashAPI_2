using System;

namespace LaundryDashAPI_2.DTOs.BookingLog
{
    public class BookingLogDTO
    {
        public Guid BookingLogId { get; set; } // Unique identifier for the booking log
        public Guid LaundryServiceLogId { get; set; } // ID of the associated laundry service log
        public string LaundryShopName { get; set; } // Name of the laundry shop
        public string LaundryShopAddress { get; set; }
        public string ServiceName { get; set; } // Name of the selected service
        public DateTime BookingDate { get; set; } // Date and time the booking was created
        public DateTime? DeliveryDate { get;set; } //added for delivery
        public decimal? TotalPrice { get; set; } // Total price of the booking
        public decimal? Weight { get; set; } // Weight of the laundry
        public string PickupAddress { get; set; } // Pickup address for the booking
        public string DeliveryAddress { get; set; } // Delivery address for the booking
        public string? Note { get; set; } // Optional note from the client
        public string ClientName { get; set; } // Full name of the client
        public string ClientNumber { get; set; }
        public string RiderNumber { get; set; }
        public string? RiderName { get; set; }//new add by gian
        public string? PaymentMethod { get; set; }

        //new
        public string BookingStatus { get; set; }


    }
}
