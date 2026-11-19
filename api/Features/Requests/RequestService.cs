using DjPortalApi.Features.Events;
using DjPortalApi.Features.Extensions;

namespace DjPortalApi.Features.Requests;

public sealed class RequestService(IRequestRepository requestRepository, IEventService eventService) : IRequestService
{
    public async Task<CreateResult> CreateAsync(Guid eventId, Guid userId, bool isAuthenticated, MusicRequestModel model)
    {
        var eventDetails = await eventService.Get(eventId);
        if (eventDetails is null)
        {
            return CreateResult.InvalidEvent("That event could not be found.");
        }

        // The DJ can add tracks to any event via the portal form, so only dancers are held to IsRequestable.
        if (!isAuthenticated && !eventDetails.IsRequestable)
        {
            return CreateResult.InvalidEvent("Requests are not currently open for this event.");
        }

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

    public async Task<IList<MusicRequest>> ListAsync(Guid eventId, Guid userId, bool isAuthenticated)
    {
        var requests = await requestRepository.Get(eventId);
        var uniqueRequests = requests
            .Distinct(new MusicRequestComparer())
            .OrderBy(x => x.StatusOrder)
            .ThenBy(x => x.UserName)
            .ThenBy(x => x.TrackName)
            .ToList();

        return isAuthenticated ? uniqueRequests : FilterForVisitor(uniqueRequests, userId).ToList();
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

        foreach (var userGuid in userIds)
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
        foreach (var request in requests)
        {
            request.UserName = names.TryGetValue(request.UserId, out var dancerName) ? dancerName : request.UserName.Obfuscate();

            if (string.Equals(RequestStatus.Pending.ToString(), request.Status) && !Equals(userId, request.UserId))
            {
                request.TrackName = "Pending Approval";
            }

            // Visitors never see user ids, including their own - "You" already marks their requests.
            request.UserId = Guid.Empty;

            yield return request;
        }
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
