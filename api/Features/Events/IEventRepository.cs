namespace DjPortalApi.Features.Events;

public interface IEventRepository
{
    Task Create(CreateEventModel eventDetails);

    Task Update(UpdateEventModel eventDetails);

    Task Delete(Guid id);

    Task<IList<EventDetails>> List();

    Task DeleteAndCreateEventIndex();
}