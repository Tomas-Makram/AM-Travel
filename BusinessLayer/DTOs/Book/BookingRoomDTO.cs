using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Book
{
    public sealed class BookingRoomDTO
    {
        public Guid? BookingRoomId { get; set; }

        public RoomType RoomType { get; set; } = RoomType.Single;

        [Range(1, 100, ErrorMessage = "Room count must be greater than zero.")]
        public int Count { get; set; } = 1;

        [Range(1, 999999999, ErrorMessage = "Night price must be greater than zero.")]
        public decimal NightPrice { get; set; }
    }
}
