using System.Net;
using DjPortalApi.Features;
using DjPortalApi.Features.AiChat;
using DjPortalApi.Features.Events;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DjPortalApi;

public class AiChatFunction(
    IAiChatService aiChatService,
    IEventService eventService) : BaseFunction
{
    [Function("AiChatMessageOptions")]
    public HttpResponseData AiChatMessageOptions([HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "aichat/message")] HttpRequestData req)
    {
        return req.CreateResponse(HttpStatusCode.OK);
    }

    [Function("AiChatMessage")]
    public async Task<HttpResponseData> AiChatMessage([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "aichat/message")] HttpRequestData req)
    {
        var model = await GetModelAsync<AiChatRequest>(req);
        if (!Guid.TryParse(model?.EventId, out var eventId) || model.Messages is not { Count: > 0 })
        {
            return CreateEmptyResponse(req, HttpStatusCode.BadRequest);
        }

        var eventDetails = await eventService.Get(eventId);
        if (eventDetails is null)
        {
            return CreateEmptyResponse(req, HttpStatusCode.BadRequest);
        }

        var existingUserCookie = GetUserCookieOrDefault(req, out var userId);
        var user = GetAuthenticatedUser(req);
        var isAuthenticated = user is { IsAuthenticated: true };

        // DJ mode is only ever honoured for an authenticated caller, so it cannot be spoofed from the public page.
        var mode = isAuthenticated && string.Equals(model.Mode, "dj", StringComparison.OrdinalIgnoreCase)
            ? AiChatMode.Dj
            : AiChatMode.Dancer;

        // The DJ can add tracks to any event via the portal form, so only dancers are held to IsRequestable.
        if (mode == AiChatMode.Dancer && !eventDetails.IsRequestable)
        {
            return CreateEmptyResponse(req, HttpStatusCode.BadRequest);
        }

        var response = await aiChatService.SendAsync(eventDetails, userId, isAuthenticated, model.Messages, mode);

        return await CreateResponseAsync(req, HttpStatusCode.OK, response, !existingUserCookie, userId);
    }
}
