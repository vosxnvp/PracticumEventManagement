using System.Text.Json.Serialization;

namespace PracticumEventManagement.Models
{
    public class Booking
    {
        private Booking()
        {
        }

        public Booking(Guid eventId)
        {
            EventId = eventId;
        }

        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public BookingStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        [JsonIgnore]
        public Event? Event { get; set; }

        public void Confirm()
        {
            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            Status = BookingStatus.Rejected;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}