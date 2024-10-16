namespace LaundryDashAPI_2.DTOs.LaundryServiceLog
{
    public class LaundryServiceLogPriceUpdateDTO
    {
        public Guid LaundryServiceLogId { get; set; } // ID of the laundry service log
        public decimal Price { get; set; } // The updated price
    }
}
