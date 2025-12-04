using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DjPortalApi;

public class RequestsFunction
{
    private readonly ILogger<RequestsFunction> _logger;

    public RequestsFunction(ILogger<RequestsFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetRequests")]
    public IActionResult GetRequests([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "musicrequest/list/")] HttpRequest req)
    {
        _logger.LogInformation("Getting all requests");
        
        // var requests = new[]
        // {
        //     new { Id = 1, Song = "Stayin' Alive", Artist = "Bee Gees", RequestedBy = "John", Status = "Pending" },
        //     new { Id = 2, Song = "Billie Jean", Artist = "Michael Jackson", RequestedBy = "Jane", Status = "Approved" },
        //     new { Id = 3, Song = "Uptown Funk", Artist = "Bruno Mars", RequestedBy = "Bob", Status = "Pending" }
        // };

        return new OkObjectResult(Array.Empty<string>());
    }

    [Function("CreateRequest")]
    public IActionResult CreateRequest([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/create")] HttpRequest req)
    {
        _logger.LogInformation("Creating new request");
        
        var newRequest = new { Id = 4, Song = "New Song", Artist = "Artist Name", RequestedBy = "User", Status = "Pending" };

        return new OkObjectResult(newRequest);
    }
}
