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
        if (eventDetails is null || !eventDetails.IsRequestable)
        {
            return CreateEmptyResponse(req, HttpStatusCode.BadRequest);
        }

        var existingUserCookie = GetUserCookieOrDefault(req, out var userId);
        var user = GetAuthenticatedUser(req);
        var isAuthenticated = user is { IsAuthenticated: true };

        var response = await aiChatService.SendAsync(eventDetails, userId, isAuthenticated, model.Messages);

        return await CreateResponseAsync(req, HttpStatusCode.OK, response, !existingUserCookie, userId);
    }
}
