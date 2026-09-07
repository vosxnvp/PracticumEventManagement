using Microsoft.AspNetCore.Mvc;
using PracticumEventManagement.Middleware;
using PracticumEventManagement.Services;
using PracticumEventManagement.BackgroundServices;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errorMessage = context.ModelState
                .SelectMany(x => x.Value?.Errors ?? [])
                .Select(x => x.ErrorMessage)
                .FirstOrDefault();

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error",
                Detail = errorMessage ?? "Validation failed."
            };

            return new BadRequestObjectResult(problemDetails);
        };
    });
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<IBookingService, BookingService>();
builder.Services.AddHostedService<BookingProcessingService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
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
