using Microsoft.AspNetCore.Mvc;
using PracticumEventManagement.Models;
using PracticumEventManagement.Services;

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
    public ActionResult<IEnumerable<Event>> GetAll()
    {
        var events = _eventService.GetAll();

        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Event> GetById(Guid id)
    {
        var eventItem = _eventService.GetById(id);

        if (eventItem is null)
        {
            return NotFound();
        }

        return Ok(eventItem);
    }

    [HttpPost]
    public ActionResult<Event> Create(Event eventItem)
    {
        var createdEvent = _eventService.Create(eventItem);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdEvent.Id },
            createdEvent);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Event eventItem)
    {
        var updated = _eventService.Update(id, eventItem);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var deleted = _eventService.Delete(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}