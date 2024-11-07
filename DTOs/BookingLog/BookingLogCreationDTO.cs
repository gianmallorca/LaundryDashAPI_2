﻿namespace LaundryDashAPI_2.DTOs.BookingLog
{
    public class BookingLogCreationDTO
    {
        public Guid LaundryServiceLogId { get; set; }
        public string LaundryShopName { get; set; }
        public string PickupAddress { get; set; }
        public string DeliveryAddress { get; set; }

        public bool? IsAccepted { get; set; }
        public bool? DepartedFromShop { get; set; }
        public bool? AcceptedByRider { get; set; }
        public bool? ReceivedByClient { get; set; }

    }
}
