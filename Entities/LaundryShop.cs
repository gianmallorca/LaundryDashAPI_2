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
        public string Address {  get; set; }

        public ICollection<LaundryServiceLog>LaundryServiceLogs { get; set; }
    }
}
