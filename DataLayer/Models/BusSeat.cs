using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class BusSeat
    {
        [Key]
        public Guid SeatId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BusId { get; set; }

        [ForeignKey(nameof(BusId))]
        public Bus Bus { get; set; } = null!;

        [Required]
        public int SeatNumber { get; set; }

        public int RowNumber { get; set; }

        public int ColumnNumber { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string? SeatLabel { get; set; }
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public SeatType SeatType { get; set; } = SeatType.Normal;
    }
}