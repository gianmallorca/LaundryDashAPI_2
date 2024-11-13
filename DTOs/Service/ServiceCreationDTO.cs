using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.DTOs.Service
{
    public class ServiceCreationDTO
    {
        [Required]
        [StringLength(50)]
        public string ServiceName { get; set; }

        public bool IsActive { get; set; }
    }
}
