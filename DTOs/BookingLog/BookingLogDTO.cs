using LaundryDashAPI_2.Validations;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.DTOs.BookingLog
{
    public class BookingLogDTO
    {
        public Guid LaundryServiceLogId { get; set; }
        public string PickupAddress { get; set; }
        public string DeliveryAddress { get; set; }
    }
}
