using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.DataAccess;
using PracticumEventManagement.Services;

namespace PracticumEventManagement.Tests.Infrastructure;

internal static class TestDbFactory
{
    public static ServiceProvider CreateProvider()
    {
        // All scopes from this provider use one database; other tests use unique names.
        var dbName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        return services.BuildServiceProvider(validateScopes: true);
    }
}
