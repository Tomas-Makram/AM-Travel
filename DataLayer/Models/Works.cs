using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class Works
    {
        [Key]
        public Guid WorkId { get; set; } = Guid.Empty;

        [Required]
        public Guid UserId { get; set; } = Guid.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string NameClient { get; set; } = string.Empty;

        [Required]
        public ClientType ClientType { get; set; } = ClientType.threeStar;

        public string Notes { get; set; } = string.Empty;

        [Required]
        public DateTime DayCreated { get; set; } = DateTime.UtcNow;

        public DateTime DayUpdated { get; set; } = DateTime.UtcNow;

    }
}