namespace LaundryDashAPI_2.DTOs.BookingProgress
{
    public class BookingProgressDTO
    {
        public bool HasStartedYourLaundry { get; set; }
        public bool IsReadyForDelivery { get; set; }
        public bool PickUpFromShop { get; set; }
        public bool DepartedFromShop { get; set; }
        public bool IsOutForDelivery { get; set; }
        public bool ReceivedByClient { get; set; }
        public bool TransactionCompleted { get; set; }

    }

}
