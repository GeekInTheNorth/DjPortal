using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using DjPortalApi.Features;
using DjPortalApi.Features.Events;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DjPortalApi;

public class EventsFunction(IEventService eventService) : BaseFunction
{
    [Function("GetEvents")]
    public async Task<HttpResponseData> GetEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/list")] HttpRequestData req)
    {
        var model = await eventService.List(DateTime.Today.AddDays(-7));
        model = model.Where(x => !x.IsCancelled).OrderByDescending(x => x.Date).ToList();

        return await CreateResponseAsync(req, System.Net.HttpStatusCode.OK, model);
    }

    [Function("CreateEvent")]
    public async Task<HttpResponseData> CreateEvent([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/create")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var principal);
        if (authResponse != null) return authResponse;

        var model = await GetModelAsync<CreateEventModel>(req);
        model ??= new CreateEventModel();

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        if (!Validator.TryValidateObject(model, context, validationResults, true))
        {
            return await CreateResponseAsync(req, System.Net.HttpStatusCode.BadRequest, validationResults);
        }

        await eventService.Create(model);

        return CreateEmptyResponse(req);
    }

    [Function("UpdateEvent")]
    public async Task<HttpResponseData> UpdateEvent([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/update")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var principal);
        if (authResponse != null) return authResponse;

        var model = await GetModelAsync<UpdateEventModel>(req);
        model ??= new UpdateEventModel();

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        if (!Validator.TryValidateObject(model, context, validationResults, true))
        {
            return await CreateResponseAsync(req, System.Net.HttpStatusCode.BadRequest, validationResults);
        }

        await eventService.Update(model);

        return CreateEmptyResponse(req);
    }

    [Function("DeleteEvent")]
    public async Task<HttpResponseData> DeleteEvent([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "events/delete")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var principal);
        if (authResponse != null) return authResponse;

        if (Guid.TryParse(req.Query["id"], out var guidId))
        {
            await eventService.Delete(guidId);

            return CreateEmptyResponse(req);
        }

        return CreateEmptyResponse(req, System.Net.HttpStatusCode.BadRequest);
    }

    [Function("DeleteAllEvents")]
    public async Task<HttpResponseData> DeleteAllEvents([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "events/deleteall")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var principal);
        if (authResponse != null) return authResponse;

        await eventService.DeleteAndCreateEventIndex();

        return CreateEmptyResponse(req);
    }

    [Function("DeleteExpiredEvents")]
    public async Task<HttpResponseData> DeleteExpiredEvents([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "events/deleteexpired")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var principal);
        if (authResponse != null) return authResponse;

        await eventService.DeleteExpiredEvents();

        return CreateEmptyResponse(req);
    }

    [Function("DeleteCache")]
    public HttpResponseData DeleteCache([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "events/deletecache")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var principal);
        if (authResponse != null) return authResponse;

        eventService.PurgeCache();

        return CreateEmptyResponse(req);
    }

    [Function("GetCalenderInvite")]
    public async Task<HttpResponseData> GetCalenderInvite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/getinvite/{id}/dance-event.ics")] HttpRequestData req,
        Guid id)
    {
        var thisEvent = await eventService.Get(id);
        if (thisEvent == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            return notFoundResponse;
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("BEGIN:VCALENDAR");
        stringBuilder.AppendLine("VERSION:2.0");
        stringBuilder.AppendLine("PRODID:cerocdjmark.azurewebsites.net");
        stringBuilder.AppendLine("CALSCALE:GREGORIAN");
        stringBuilder.AppendLine("METHOD:PUBLISH");
        stringBuilder.AppendLine("BEGIN:VEVENT");
        stringBuilder.AppendLine($"DTSTART:{thisEvent.StartTime:yyyyMMddTHHmm00}");
        stringBuilder.AppendLine($"DTEND:{thisEvent.EndTime:yyyyMMddTHHmm00}");
        stringBuilder.AppendLine($"SUMMARY:{thisEvent.Name}");
        stringBuilder.AppendLine($"LOCATION:{thisEvent.LocationName}");
        stringBuilder.AppendLine($"DESCRIPTION:{thisEvent.Name} at {thisEvent.LocationName} ({thisEvent.LocationAddress})");
        stringBuilder.AppendLine("PRIORITY:3");
        stringBuilder.AppendLine("END:VEVENT");
        stringBuilder.AppendLine("END:VCALENDAR");

        var calendarContent = Encoding.UTF8.GetBytes(stringBuilder.ToString());
        var fileName = "dance-event.ics";

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/calendar; charset=utf-8");
        response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
        await response.Body.WriteAsync(calendarContent);

        return response;
    }
}
