using System;

namespace LaundryDashAPI_2.DTOs.BookingLog
{
    public class BookingLogDTO
    {
        public Guid BookingLogId { get; set; } // Unique identifier for the booking log
        public Guid LaundryServiceLogId { get; set; } // ID of the associated laundry service log
        public string LaundryShopName { get; set; } // Name of the laundry shop
        public string ServiceName { get; set; } // Name of the selected service
        public DateTime BookingDate { get; set; } // Date and time the booking was created
        public decimal? TotalPrice { get; set; } // Total price of the booking
        public decimal? Weight { get; set; } // Weight of the laundry
        public string PickupAddress { get; set; } // Pickup address for the booking
        public string DeliveryAddress { get; set; } // Delivery address for the booking
        public string? Note { get; set; } // Optional note from the client
        public string ClientName { get; set; } // Full name of the client

        // Booking status flags
        //public bool IsAcceptedByShop { get; set; } // Indicates if the shop has accepted the booking
        //public bool PickUpFromClient { get; set; } // Indicates if the laundry has been picked up from the client
        //public bool HasStartedYourLaundry { get; set; } // Indicates if the laundry process has started
        //public bool IsReadyForDelivery { get; set; } // Indicates if the laundry is ready for delivery
        //public bool PickUpFromShop { get; set; } // Indicates if the rider has picked up the laundry from the shop
        //public bool DepartedFromShop { get; set; } // Indicates if the rider has departed the shop with the laundry
        //public bool IsOutForDelivery { get; set; } // Indicates if the laundry is out for delivery
        //public bool ReceivedByClient { get; set; } // Indicates if the client has received the laundry
        //public bool TransactionCompleted { get; set; } // Indicates if the booking transaction is completed
    }
}
