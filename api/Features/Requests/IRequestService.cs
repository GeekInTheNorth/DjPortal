namespace DjPortalApi.Features.Requests;

public interface IRequestService
{
    Task<CreateResult> CreateAsync(Guid eventId, Guid userId, bool isAuthenticated, MusicRequestModel model);
}
