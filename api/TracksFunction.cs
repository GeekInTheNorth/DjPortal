using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DjPortalApi;

public class TracksFunction
{
    private readonly ILogger<TracksFunction> _logger;

    public TracksFunction(ILogger<TracksFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetTracks")]
    public IActionResult GetTracks([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tracks")] HttpRequest req)
    {
        _logger.LogInformation("Getting all tracks");
        
        var tracks = new[]
        {
            new { Id = 1, Title = "One More Time", Artist = "Daft Punk", Genre = "Electronic", Duration = "5:20" },
            new { Id = 2, Title = "Around The World", Artist = "Daft Punk", Genre = "Electronic", Duration = "7:09" },
            new { Id = 3, Title = "Get Lucky", Artist = "Daft Punk", Genre = "Electronic", Duration = "6:09" },
            new { Id = 4, Title = "Levels", Artist = "Avicii", Genre = "EDM", Duration = "6:17" },
            new { Id = 5, Title = "Wake Me Up", Artist = "Avicii", Genre = "EDM", Duration = "4:09" }
        };

        return new OkObjectResult(tracks);
    }
}
