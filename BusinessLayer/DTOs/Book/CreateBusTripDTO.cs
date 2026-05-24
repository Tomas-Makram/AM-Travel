using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Book
{
    public class CreateBusTripDTO
    {
        [Required(ErrorMessage = "Bus is required.")]
        public Guid BusId { get; set; }

        [Required(ErrorMessage = "Trip direction is required.")]
        public TripDirection Direction { get; set; } = TripDirection.Departure;

        [Required(ErrorMessage = "Trip date is required.")]
        [DataType(DataType.Date)]
        public DateTime TripDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "From location is required.")]
        [MaxLength(200)]
        public string? FromLocation { get; set; }

        [Required(ErrorMessage = "To location is required.")]
        [MaxLength(200)]
        public string? ToLocation { get; set; }
    }
}
