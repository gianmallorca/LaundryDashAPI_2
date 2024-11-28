using LaundryDashAPI_2.Validations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.Entities
{
    public class BookingLog
    {
        public Guid BookingLogId { get; set; }

        public Guid LaundryServiceLogId { get; set; }
        public LaundryServiceLog? LaundryServiceLog { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? Weight { get; set; }

        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(120)]
        [FirstLetterUppercase]
        public string PickupAddress { get; set; }

        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(120)]
        [FirstLetterUppercase]
        public string DeliveryAddress { get; set; }

        public string? Note { get; set; }

        public string ClientId { get; set; }


        public bool IsAcceptedByShop { get; set; }
        public bool PickUpFromClient { get; set; }

        public bool HasStartedYourLaundry { get; set; }//shop  ---
        public bool IsReadyForDelivery { get; set; }//shop
        public bool PickUpFromShop { get; set; }//rider
        public bool DepartedFromShop { get; set; }//shop  ---
        public bool IsOutForDelivery { get; set; }//rider ---
        public bool ReceivedByClient { get; set; }//rider ---
        public bool TransactionCompleted { get; set; }//rider ---

        
        public string? PickupRiderId { get; set; }
        public string? DeliveryRiderId { get; set; }
    }
}
