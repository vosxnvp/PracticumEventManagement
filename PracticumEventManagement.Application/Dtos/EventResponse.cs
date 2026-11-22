using PracticumEventManagement.Domain.Entities;

namespace PracticumEventManagement.Application.Dtos;

public sealed record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats)
{
    public static EventResponse FromEvent(Event eventItem)
    {
        return new EventResponse(
            eventItem.Id,
            eventItem.Title,
            eventItem.Description,
            eventItem.StartAt,
            eventItem.EndAt,
            eventItem.TotalSeats,
            eventItem.AvailableSeats);
    }
}