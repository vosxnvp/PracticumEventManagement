using Microsoft.AspNetCore.Mvc;
using PracticumEventManagement.Models;
using PracticumEventManagement.Services;
using PracticumEventManagement.Dtos;

namespace PracticumEventManagement.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Event>> GetAll(
    [FromQuery] string? title,
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to)
    {
        var events = _eventService.GetAll(title, from, to);

        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Event> GetById(Guid id)
    {
        var eventItem = _eventService.GetById(id);

         return Ok(eventItem);
    }

    [HttpPost]
    public ActionResult<Event> Create(CreateEventRequest request)
    {
        var eventItem = new Event
        {
            Title = request.Title!,
            Description = request.Description,
            StartAt = request.StartAt!.Value,
            EndAt = request.EndAt!.Value
        };

        var createdEvent = _eventService.Create(eventItem);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdEvent.Id },
            createdEvent);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, UpdateEventRequest request)
    {
        var eventItem = new Event
        {
            Title = request.Title!,
            Description = request.Description,
            StartAt = request.StartAt!.Value,
            EndAt = request.EndAt!.Value
        };

        var updated = _eventService.Update(id, eventItem);


        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var deleted = _eventService.Delete(id);

    

        return NoContent();
    }
}