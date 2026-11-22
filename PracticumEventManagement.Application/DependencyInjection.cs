using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.Application.Interfaces;
using PracticumEventManagement.Application.Services;

namespace PracticumEventManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingProcessor, BookingProcessor>();

        return services;
    }
}