using LaundryDashAPI_2.Validations;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.Entities
{
    public class BookingLog
    {
        public Guid BookingLogId { get; set; }


        public Guid LaundryServiceLogId { get; set; }
        public LaundryServiceLog LaundryServiceLog { get; set; }


        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(120)]
        [FirstLetterUppercase]
        public string PickupAddress { get; set; }

        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(120)]
        [FirstLetterUppercase]
        public string DeliveryAddress { get; set; }
        public string ClientId { get; set; }
        public bool ?IsAccepted { get; set; }
    }
}
