using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.DataAccess;
using PracticumEventManagement.Models;
using PracticumEventManagement.Tests.Infrastructure;

namespace PracticumEventManagement.Tests;

public class AppDbContextTests
{
    [Fact]
    public async Task Context_ShouldShareDataAcrossScopes()
    {
        // Arrange
        using var provider = TestDbFactory.CreateProvider();

        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            context.Events.Add(new Event("Test event")
            {
                Id = eventId,
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(2),
                TotalSeats = 2,
                AvailableSeats = 2
            });

            context.Bookings.Add(new Booking(eventId)
            {
                Id = bookingId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }

        // Act
        using var readScope = provider.CreateScope();

        var readContext = readScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var eventItem = await readContext.Events
            .Include(e => e.Bookings)
            .SingleAsync(e => e.Id == eventId);

        // Assert
        Assert.Equal("Test event", eventItem.Title);
        Assert.Single(eventItem.Bookings);
        Assert.Equal(bookingId, eventItem.Bookings.Single().Id);
    }
}