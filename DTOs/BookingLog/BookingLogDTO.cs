using LaundryDashAPI_2.Validations;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.DTOs.BookingLog
{
    public class BookingLogDTO
    {
        public Guid BookingLogId { get; set; }
        public Guid LaundryServiceLogId { get; set; }
        public string LaundryShopName { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? Weight { get; set; }
        public string PickupAddress { get; set; }
        public string DeliveryAddress { get; set; }
        public string? Note { get; set; }

  

  

        // Booking status
        public bool IsAcceptedByShop { get; set; }
        public bool PickUpFromClient { get; set; }
        public bool HasStartedYourLaundry { get; set; }
        public bool IsReadyForDelivery { get; set; }
        public bool PickUpFromShop { get; set; }
        public bool DepartedFromShop { get; set; }
        public bool IsOutForDelivery { get; set; }
        public bool ReceivedByClient { get; set; }
        public bool TransactionCompleted { get; set; }
    }

}
