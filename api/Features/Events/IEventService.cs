namespace DjPortalApi.Features.Events;

public interface IEventService
{
    Task Create(CreateEventModel eventDetails);

    Task Update(UpdateEventModel eventDetails);

    Task Delete(Guid id);

    Task DeleteAndCreateEventIndex();

    Task DeleteExpiredEvents();

    Task<EventDetails?> Get(Guid id);

    Task<IList<EventDetails>> List(DateTime oldestDate, int size = 100);

    void PurgeCache();
}
