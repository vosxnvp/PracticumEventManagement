using Microsoft.AspNetCore.Mvc;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Models;
using PracticumEventManagement.Services;

namespace PracticumEventManagement.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public EventsController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService;
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<Event>>> GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _eventService.GetAllAsync(
            title, from, to, page, pageSize, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Event>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var eventItem = await _eventService.GetByIdAsync(id, cancellationToken);
        return Ok(eventItem);
    }

    [HttpPost]
    public async Task<ActionResult<Event>> Create(
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventItem = new Event(request.Title!)
        {
            Description = request.Description,
            StartAt = request.StartAt!.Value,
            EndAt = request.EndAt!.Value,
            TotalSeats = request.TotalSeats!.Value
        };

        var createdEvent = await _eventService.CreateAsync(eventItem, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
    }

    [HttpPost("{id:guid}/book")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingInfo>> Book(Guid id)
    {
        var booking = await _bookingService.CreateBookingAsync(id);
        return Accepted($"/bookings/{booking.Id}", booking);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventItem = new Event(request.Title!)
        {
            Description = request.Description,
            StartAt = request.StartAt!.Value,
            EndAt = request.EndAt!.Value
        };

        await _eventService.UpdateAsync(id, eventItem, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _eventService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
