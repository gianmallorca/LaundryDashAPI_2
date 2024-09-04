using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.DTOs
{
    public class ServiceCreationDTO
    {
        [Required]
        [StringLength(50)]
        public string ServiceName { get; set; }
    }
}
