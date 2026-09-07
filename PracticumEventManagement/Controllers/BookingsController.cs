using Microsoft.AspNetCore.Mvc;
using PracticumEventManagement.Dtos;
using PracticumEventManagement.Services;

namespace PracticumEventManagement.Controllers;

[ApiController]
[Route("bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingInfo>> GetById(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        return Ok(booking);
    }
}