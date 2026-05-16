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

        // Filter and obfuscate for non-authenticated users
        var user = GetAuthenticatedUser(req);
        if (user is not { IsAuthenticated: true })
        {
            uniqueRequests = FilterForVisitor(uniqueRequests, userId).ToList();
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

        // Customers can only request a limited number of tracks.
        // Customers are never authenticated.
        var user = GetAuthenticatedUser(req);
        if (user is not { IsAuthenticated: true })
        {
            var requestCount = await requestRepository.GetCountByUserAndEvent(eventId, userId);
            if (requestCount >= AppConstants.MaxAnonymousRequestsPerEvent)
            {
                return await CreateResponseAsync(req, System.Net.HttpStatusCode.Conflict, new { message = AppConstants.MaxRequestsExceededMessage }, !existingUserCookie, userId);
            }
        }

        var newRequest = new MusicRequest
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            UserName = model.RequestedBy,
            TrackName = model.MusicRequest
        };

        // The DJ gets an auto approval on new requests and a random user id to make each request a uniquely owned request
        if (user is { IsAuthenticated: true})
        {
            newRequest.Status = RequestStatus.Approved.ToString();
            newRequest.UserId = Guid.NewGuid();
        }

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

    private static IEnumerable<MusicRequest> FilterForVisitor(IList<MusicRequest> requests, Guid userId)
    {
        var dancingNames = new List<string>
        {
            "Ballroom Ghost",
            "Dancefloor Poet",
            "Dancing Queen",
            "Disco Voyager",
            "Groove Bandit",
            "Harmony Rebel",
            "Moonlight Mover",
            "Midnight Rider",
            "Rhythm Renegade",
            "Son of a Preacher Man",
            "Spin Me Slowly",
            "Sweet Caroline",
            "Sweet Child of Mine",
            "The One & Only",
            "Tiny Dancer"
        };
        
        dancingNames.Shuffle();
        var userIds = requests.Select(x => x.UserId).Distinct();
        var names = new Dictionary<Guid, string>();
        var item = 0;

        foreach(var userGuid in userIds)
        {
            if (Equals(userId, userGuid))
            {
                names.Add(userGuid, "You");
            }
            else
            {
                names.Add(userGuid, dancingNames[item]);
                item++;
            }

            if (item >= dancingNames.Count)
            {
                item = 0;
            }
        }

        // Non-authenticated users should only see obfuscated names
        // Unapproved requests should be anonymised except to the requester
        foreach(var request in requests)
        {
            request.UserName = names.TryGetValue(request.UserId, out var dancerName) ? dancerName : request.UserName.Obfuscate();

            if (string.Equals(RequestStatus.Pending.ToString(), request.Status) && !Equals(userId, request.UserId))
            {
                request.TrackName = "Pending Approval";
            }

            yield return request;
        }
    }
}
