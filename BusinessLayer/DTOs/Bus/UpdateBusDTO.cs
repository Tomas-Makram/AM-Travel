using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Bus
{
    public class UpdateBusDTO
    {
        [Required]
        public Guid BusId { get; set; }

        [Required(ErrorMessage = "Bus name is required")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PlateNumber { get; set; }

        [Required]
        public int LayoutRows { get; set; } = 12;

        [Required]
        public int LayoutColumns { get; set; } = 5;

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
    }
}