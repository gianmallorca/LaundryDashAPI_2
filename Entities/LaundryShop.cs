using LaundryDashAPI_2.Validations;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.Entities
{
    public class LaundryShop
    {
        public Guid LaundryShopId { get; set; }
        [Required(ErrorMessage = "The field with name {0} is required")]
        [StringLength(120)]
        [FirstLetterUppercase]

        public string LaundryShopName { get; set; }
        [Required(ErrorMessage = "This field is required!")]
        public string City { get; set; }
        [Required(ErrorMessage = "This field is required!")]
        public string Barangay { get; set; }
        [Required(ErrorMessage = "This field is required!")]
        public string BrgyStreet { get; set; }

        public string Address
        {
            get
            {
                return $"{City},{Barangay}, {BrgyStreet}";
            }
        }

        public string? ContactNum { get; set; }
        public string? TimeOpen { get; set; }
        public string? TimeClose { get; set; }
        public bool? Monday { get; set; }
        public bool? Tuesday { get; set; }
        public bool? Wednesday { get; set; }
        public bool? Thursday { get; set; }
        public bool? Friday { get; set; }
        public bool? Saturday { get; set; }
        public bool? Sunday { get; set; }

        public string? AddedById { get; set; }

        public ICollection<LaundryServiceLog>LaundryServiceLogs { get; set; }
    }
}
