namespace DjPortalApi.Features.Requests;

public interface IRequestService
{
    Task<CreateResult> CreateAsync(Guid eventId, Guid userId, bool isAuthenticated, MusicRequestModel model);

    /// <summary>
    /// Lists an event's requests. Visitors get them anonymised: pseudonyms for other dancers, "You" for
    /// their own, hidden track names on others' pending requests, and no user ids.
    /// </summary>
    Task<IList<MusicRequest>> ListAsync(Guid eventId, Guid userId, bool isAuthenticated);
}
