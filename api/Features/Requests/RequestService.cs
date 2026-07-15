using DjPortalApi.Features.Spotify;

namespace DjPortalApi.Features.Requests;

public sealed class RequestService(
    IRequestRepository requestRepository,
    ISpotifyService spotifyService) : IRequestService
{
    public async Task<CreateResult> CreateAsync(Guid eventId, Guid userId, bool isAuthenticated, MusicRequestModel model)
    {
        // Customers (never authenticated) can only request a limited number of tracks.
        if (!isAuthenticated)
        {
            var requestCount = await requestRepository.GetCountByUserAndEvent(eventId, userId);
            if (requestCount >= AppConstants.MaxAnonymousRequestsPerEvent)
            {
                return CreateResult.QuotaExceeded(AppConstants.MaxRequestsExceededMessage);
            }
        }

        var newRequest = new MusicRequest
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            UserName = model.RequestedBy,
            TrackName = model.MusicRequest,
            BPM = model.SafeBpm,
            Time = model.Time,
            IsFinalized = true
        };

        // The DJ gets an auto approval on new requests and a random user id to make each request a uniquely owned request
        if (isAuthenticated)
        {
            newRequest.Status = RequestStatus.Approved.ToString();
            newRequest.UserId = Guid.NewGuid();
        }

        newRequest = await ProcessSpotifyUrl(newRequest);
        await requestRepository.Add(newRequest);

        return CreateResult.Created(newRequest);
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
