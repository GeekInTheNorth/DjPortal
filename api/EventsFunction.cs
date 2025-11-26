using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DjPortalApi;

public class EventsFunction
{
    private readonly ILogger<EventsFunction> _logger;

    public EventsFunction(ILogger<EventsFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetEvents")]
    public IActionResult GetEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events")] HttpRequest req)
    {
        _logger.LogInformation("Getting all events");
        
        var events = new[]
        {
            new { Id = 1, Name = "Summer Music Festival", Date = "2025-07-15", Venue = "Central Park" },
            new { Id = 2, Name = "DJ Night", Date = "2025-08-20", Venue = "Club Underground" },
            new { Id = 3, Name = "Electronic Music Conference", Date = "2025-09-10", Venue = "Convention Center" }
        };

        return new OkObjectResult(events);
    }

    [Function("CreateEvent")]
    public IActionResult CreateEvent([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/create")] HttpRequest req)
    {
        _logger.LogInformation("Creating new event");
        
        var newEvent = new { Id = 4, Name = "New Event", Date = "2025-10-01", Venue = "TBD", Status = "Created" };

        return new OkObjectResult(newEvent);
    }
}
