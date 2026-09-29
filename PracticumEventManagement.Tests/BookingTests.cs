using PracticumEventManagement.Models;

namespace PracticumEventManagement.Tests;

public class BookingTests
{
    [Fact]
    public void Confirm_ShouldSetConfirmedStatusAndProcessedAt()
    {
        // Arrange
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Reject_ShouldSetRejectedStatusAndProcessedAt()
    {
        // Arrange
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
}