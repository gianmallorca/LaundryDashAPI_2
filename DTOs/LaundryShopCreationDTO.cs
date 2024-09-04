using LaundryDashAPI_2.Validations;
using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.DTOs
{
    public class LaundryShopCreationDTO
    {
        [Required(ErrorMessage = "The field with name {0} is required")]
        [StringLength(120)]
        [FirstLetterUppercase]
        public string LaundryShopName { get; set; }
        public string Address { get; set; }
    }
}
