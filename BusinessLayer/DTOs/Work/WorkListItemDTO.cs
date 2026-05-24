using DataLayer.Models;

namespace BusinessLayer.DTOs.Work
{
    public class WorkListItemDTO
    {
        public Guid WorkId { get; set; }
        public Guid UserId { get; set; }

        public string? UserName { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;
        public string NameClient { get; set; } = string.Empty;

        public ClientType ClientType { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime DayCreated { get; set; }
        public DateTime DayUpdated { get; set; }
    }
}