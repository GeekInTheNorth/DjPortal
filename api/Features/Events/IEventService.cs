namespace DjPortalApi.Features.Events;

public interface IEventService
{
    Task Create(CreateEventModel eventDetails);

    Task Update(UpdateEventModel eventDetails);

    Task Delete(Guid id);

    Task DeleteAndCreateEventIndex();

    Task UpdateEventIndex();

    Task<EventDetails?> Get(Guid id);

    Task<IList<EventDetails>> List(bool includeExpired = false);

    void PurgeCache();
}
