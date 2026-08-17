using DjPortalApi.Features.Extensions;

namespace DjPortalApi.Features.Requests;

public sealed class RequestService(IRequestRepository requestRepository) : IRequestService
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

        ApplyLink(newRequest);
        await requestRepository.Add(newRequest);

        return CreateResult.Created(newRequest);
    }

    /// <summary>
    /// A request that is nothing but a URL is stored as a link, with the track name replaced by the
    /// domain so the DJ can see where it points without following it.
    /// </summary>
    private static void ApplyLink(MusicRequest request)
    {
        if (request.TrackName.TryGetLink(out var url, out var domain))
        {
            request.LinkUrl = url;
            request.TrackName = $"Link: {domain}";
        }
    }
}
