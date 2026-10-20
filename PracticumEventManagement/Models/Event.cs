using System.Text.Json.Serialization;

namespace PracticumEventManagement.Models;

public class Event
{
    private Event()
    {
    }

    public Event(string title)
    {
        Title = title;
    }

    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    [JsonIgnore]
    public ICollection<Booking> Bookings { get; } = new List<Booking>();

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;

        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;
    }
}