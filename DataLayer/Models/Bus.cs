using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class Bus
    {
        [Key]
        public Guid BusId { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PlateNumber { get; set; }

        [Range(1, 100)]
        public int SeatsCount { get; set; }

        [Required]
        public int LayoutRows { get; set; } = 4;

        [Required]
        public int LayoutColumns { get; set; } = 5;

        [MaxLength(8000)]
        public string? LayoutJson { get; set; }

        [Required]
        [MaxLength(100)]
        public string? FromLocation { get; set; }

        [Required]
        [MaxLength(100)]
        public string? ToLocation { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<BusSeat> Seats { get; set; } = new List<BusSeat>();
        public ICollection<BusTrip> Trips { get; set; } = new List<BusTrip>();
    }
}