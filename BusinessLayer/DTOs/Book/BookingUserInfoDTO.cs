namespace BusinessLayer.DTOs.Book
{
    namespace BusinessLayer.DTOs.Book
    {
        public sealed class BookingUserInfoDTO
        {
            public Guid BookingId { get; set; }
            public Guid UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
        }
    }
}
