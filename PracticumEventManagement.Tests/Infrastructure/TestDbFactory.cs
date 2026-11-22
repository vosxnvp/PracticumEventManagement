using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.Infrastructure.DataAccess;
using PracticumEventManagement.Application.Services;
using PracticumEventManagement.Application.Interfaces;
using PracticumEventManagement.Infrastructure.DataAccess.Repositories;

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
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
		services.AddScoped<IBookingProcessor, BookingProcessor>();

		return services.BuildServiceProvider(validateScopes: true);
    }
}
