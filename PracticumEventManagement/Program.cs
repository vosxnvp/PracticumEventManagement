using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticumEventManagement.Application;
using PracticumEventManagement.BackgroundServices;
using PracticumEventManagement.Domain.Enums;
using PracticumEventManagement.Infrastructure;
using PracticumEventManagement.Infrastructure.DataAccess;
using PracticumEventManagement.Middleware;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Application и Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// HTTP и JSON
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

// Фоновая обработка бронирований
builder.Services.AddHostedService<BookingProcessingService>();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Применение миграций при запуске
await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await context.Database.MigrateAsync();
}

// Обработка исключений
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "PracticumEventManagement v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();