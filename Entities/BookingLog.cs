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
        public ApplicationUser? AppUser { get; set; }


        public bool IsAcceptedByShop { get; set; }
        public bool PickUpFromClient { get; set; }
        public bool HasStartedYourLaundry { get; set; }
        public bool IsReadyForDelivery { get; set; }
        public bool PickUpFromShop { get; set; }
        public bool DepartedFromShop { get; set; }
        public bool IsOutForDelivery { get; set; }
        public bool ReceivedByClient { get; set; }
        public bool TransactionCompleted { get; set; }

        
        public string? PickupRiderId { get; set; }
        public string? DeliveryRiderId { get; set; }
    }
}
