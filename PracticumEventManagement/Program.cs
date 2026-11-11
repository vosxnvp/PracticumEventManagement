using Microsoft.AspNetCore.Mvc;
using PracticumEventManagement.Middleware;
using PracticumEventManagement.Services;
using PracticumEventManagement.BackgroundServices;
using Microsoft.EntityFrameworkCore;
using PracticumEventManagement.DataAccess;
using System.Text.Json.Serialization;
using PracticumEventManagement.Models;
using PracticumEventManagement.DataAccess.Repositories;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.

builder.Services
    .AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.Converters.Add(
             new JsonStringEnumConverter<BookingStatus>());
     })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(
                context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error"
            };

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes =
            {
                "application/problem+json"
            }
            };
        };
    });

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddHostedService<BookingProcessingService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();


    await context.Database.MigrateAsync();
}
app.UseMiddleware<ExceptionHandlingMiddleware>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "PracticumEventManagement v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
