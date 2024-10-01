using System.ComponentModel.DataAnnotations;

namespace LaundryDashAPI_2.Entities
{
    public class Service
    {
        public Guid ServiceId { get; set; }
        [Required]
        [StringLength(50)]
        public string ServiceName { get; set; }

    }
}
