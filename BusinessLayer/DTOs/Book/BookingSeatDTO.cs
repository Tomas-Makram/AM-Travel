using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Book
{
    public sealed class BookingSeatDTO
    {
        public Guid? Id { get; set; }

        public TripDirection Direction { get; set; }

        public Guid TripId { get; set; }

        public Guid SeatId { get; set; }

        [Range(0, 999999999)]
        public decimal SeatPrice { get; set; }


        // Seat Details
        public int SeatNumber { get; set; }
        public string SeatLabel { get; set; } = string.Empty;
        public SeatType SeatType { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }

        // Trip Details
        public DateTime TripDate { get; set; }

        [Display(Name = "From Location")]
        public string FromLocation { get; set; } = string.Empty;

        [Display(Name = "To Location")]
        public string ToLocation { get; set; } = string.Empty;

        // Bus Details
        public Guid BusId { get; set; }
        
        [Display(Name ="Bus Name")]
        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public bool IsTransferredToCompany { get; set; } = false;
        public string? TransferredToCompanyName { get; set; }
        public string? TransferredToCompanyPhone { get; set; }
        public Guid? CompanySeatBookingId { get; set; }
    }
}