using DjPortalApi.Features;
using DjPortalApi.Features.Requests;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DjPortalApi;

public class MusicRequestFunction(
    IRequestRepository requestRepository,
    IRequestService requestService) : BaseFunction
{
    [Function("GetMusicRequests")]
    public async Task<HttpResponseData> GetMusicRequests([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "musicrequest/list/")] HttpRequestData req)
    {
        var existingUserCookie = GetUserCookieOrDefault(req, out var userId);
        var parseSuccess = Guid.TryParse(req.Query["eventId"], out var eventId);
        if (!parseSuccess)
        {
            return await CreateResponseAsync(req, System.Net.HttpStatusCode.OK, Array.Empty<MusicRequest>(), !existingUserCookie, userId);
        }

        // Visitors get the list anonymised; the DJ sees everything.
        var user = GetAuthenticatedUser(req);
        var uniqueRequests = await requestService.ListAsync(eventId, userId, user is { IsAuthenticated: true });

        return await CreateResponseAsync(req, System.Net.HttpStatusCode.OK, uniqueRequests, !existingUserCookie, userId);
    }

    [Function("CreateRequest")]
    public async Task<HttpResponseData> CreateRequest([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "musicrequest/create")] HttpRequestData req)
    {
        var model = await GetModelAsync<MusicRequestModel>(req);
        if (!Guid.TryParse(model?.EventId, out var eventId) || string.IsNullOrWhiteSpace(model.MusicRequest) || string.IsNullOrWhiteSpace(model.RequestedBy))
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.BadRequest);
        }

        var existingUserCookie = GetUserCookieOrDefault(req, out var userId);

        // Customers can only request a limited number of tracks. Customers are never authenticated.
        var user = GetAuthenticatedUser(req);
        var isAuthenticated = user is { IsAuthenticated: true };

        var result = await requestService.CreateAsync(eventId, userId, isAuthenticated, model);
        if (result.Outcome == CreateRequestOutcome.InvalidEvent)
        {
            return await CreateResponseAsync(req, System.Net.HttpStatusCode.BadRequest, new { message = result.Message }, !existingUserCookie, userId);
        }

        if (result.Outcome == CreateRequestOutcome.QuotaExceeded)
        {
            return await CreateResponseAsync(req, System.Net.HttpStatusCode.Conflict, new { message = result.Message }, !existingUserCookie, userId);
        }

        return CreateEmptyResponse(req, System.Net.HttpStatusCode.OK, !existingUserCookie, userId);
    }

    [Function("UpdateRequestStatus")]
    public async Task<HttpResponseData> UpdateRequestStatus([HttpTrigger(AuthorizationLevel.Function, "post", Route = "musicrequest/updatestatus")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var _);
        if (authResponse != null) return authResponse;

        var model = await GetModelAsync<MusicRequestStatusUpdateModel>(req);
        if (!Guid.TryParse(model?.RequestId, out var requestId) || !Enum.TryParse<RequestStatus>(model?.Status, true, out var status))
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.BadRequest);
        }

        await requestRepository.UpdateStatus(requestId, status);

        return CreateEmptyResponse(req, System.Net.HttpStatusCode.OK);
    }

    [Function("UpdateRequestTrack")]
    public async Task<HttpResponseData> UpdateRequestTrack([HttpTrigger(AuthorizationLevel.Function, "post", Route = "musicrequest/updatetrack")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var _);
        if (authResponse != null) return authResponse;

        var model = await GetModelAsync<MusicRequestTrackUpdateModel>(req);
        if (!Guid.TryParse(model?.RequestId, out var requestId) || string.IsNullOrWhiteSpace(model?.TrackName))
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.BadRequest);
        }

        await requestRepository.UpdateTrack(requestId, model.TrackName, model.SafeBpm, model.Time);

        return CreateEmptyResponse(req, System.Net.HttpStatusCode.OK);
    }

    [Function("DeleteRequest")]
    public async Task<HttpResponseData> DeleteRequest([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "musicrequest/delete")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var _);
        if (authResponse != null) return authResponse;

        var parseSuccess = Guid.TryParse(req.Query["requestId"], out var requestId);
        if (!parseSuccess)
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.BadRequest);
        }

        await requestRepository.Delete(requestId);

        return CreateEmptyResponse(req, System.Net.HttpStatusCode.OK);
    }

    [Function("DeleteAllRequests")]
    public async Task<HttpResponseData> DeleteAllRequests([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "musicrequest/deleteall")] HttpRequestData req)
    {
        // Check if user is authenticated
        var authResponse = RequireAuthentication(req, out var _);
        if (authResponse != null) return authResponse;

        await requestRepository.DeleteAndCreateRequestIndex();

        return CreateEmptyResponse(req, System.Net.HttpStatusCode.OK);
    }
}
