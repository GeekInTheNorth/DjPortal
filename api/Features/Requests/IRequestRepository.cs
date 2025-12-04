namespace DjPortalApi.Features.Requests;

public interface IRequestRepository
{
    Task<IList<MusicRequest>> Get(Guid eventId);

    Task DeleteAll(Guid eventId);

    Task Add(MusicRequest request);

    Task UpdateStatus(Guid requestId, RequestStatus status);

    Task DeleteAndCreateRequestIndex();
}