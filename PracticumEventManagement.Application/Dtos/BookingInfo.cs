using PracticumEventManagement.Domain.Enums;

namespace PracticumEventManagement.Application.Dtos;

public class BookingInfo
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public BookingStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}

