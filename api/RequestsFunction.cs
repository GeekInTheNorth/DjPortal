using DjPortalApi.Features;
using DjPortalApi.Features.Events;
using DjPortalApi.Features.Extensions;
using DjPortalApi.Features.Requests;
using DjPortalApi.Features.Spotify;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DjPortalApi;

public class MusicRequestFunction(
    IRequestRepository requestRepository, 
    IEventRepository eventRepository,
    ISpotifyService spotifyService) : BaseFunction
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

        var requests = await requestRepository.Get(eventId);
        var comparer = new MusicRequestComparer();
        var uniqueRequests = requests.Distinct(comparer).OrderBy(x => x.StatusOrder).ThenBy(x => x.UserName).ThenBy(x => x.TrackName).ToList();

        var user =GetAuthenticatedUser(req);
        if (user is not { IsAuthenticated: true })
        {
            foreach (var musicRequest in uniqueRequests)
            {
                musicRequest.UserName = userId.Equals(musicRequest.UserId) ? "You" : musicRequest.UserName.Obfuscate();
            }
        }

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
        var newRequest = new MusicRequest
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            UserName = model.RequestedBy,
            TrackName = model.MusicRequest
        };

        newRequest = await ProcessSpotifyUrl(newRequest);
        await requestRepository.Add(newRequest);

        return CreateEmptyResponse(req, System.Net.HttpStatusCode.OK, !existingUserCookie, userId);
    }

    [Function("ShareRequest")]
    public async Task<HttpResponseData> ShareRequest([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "musicrequest/share")] HttpRequestData req)
    {
        var model = await GetModelAsync<ShareModel>(req);
        if (string.IsNullOrWhiteSpace(model?.Url))
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.BadRequest);
        }

        var existingUserCookie = GetUserCookieOrDefault(req, out var userId);
        var events = await eventRepository.List();
        
        var currentEvent = events.FirstOrDefault(x => x.IsRequestable);
        if (currentEvent != null)
        {
            var newRequest = new MusicRequest
            {
                Id = Guid.NewGuid(),
                EventId = currentEvent.Id,
                UserId = userId,
                UserName = "Shared Link",
                TrackName = model.Url
            };

            newRequest = await ProcessSpotifyUrl(newRequest);
            await requestRepository.Add(newRequest);
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

    private async Task<MusicRequest> ProcessSpotifyUrl(MusicRequest request)
    {
        if (spotifyService.TryGetSpotifyId(request.TrackName, out var trackId))
        {
            request.SpotifyUrl = request.TrackName;
            var spotifyTrack = await spotifyService.GetTrack(trackId);
            if (spotifyTrack != null)
            {
                var songName = spotifyTrack.Name;
                var artistList = spotifyTrack.Artists?.Select(x => x.Name).ToList();
                var artist = artistList != null ? string.Join(", ", artistList) : "Unknown Artist";
                request.TrackName = $"{songName} - {artist}";
            }
            else
            {
                request.TrackName = "See Link:";
            }
        }

        return request;
    }
}
