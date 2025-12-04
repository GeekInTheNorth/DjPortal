using Microsoft.Extensions.Caching.Memory;

namespace DjPortalApi.Features.Events;

public sealed class EventService(
    IEventRepository eventRepository, 
    // IRequestRepository requestRepository, 
    IMemoryCache cache) : IEventService
{
    public const string CacheKey = "dj.events.list";

    public async Task Create(CreateEventModel eventDetails)
    {
        await eventRepository.Create(eventDetails);
        cache.Remove(CacheKey);
    }

    public async Task Update(UpdateEventModel eventDetails)
    {
        await eventRepository.Update(eventDetails);
        cache.Remove(CacheKey);
    }

    public async Task Delete(Guid id)
    {
        await eventRepository.Delete(id);
        // await requestRepository.DeleteAll(id);

        cache.Remove(CacheKey);
    }

    public async Task DeleteExpiredEvents()
    {
        var allEvents = await GetCachedEventList();
        var expiredEvents = allEvents.Where(x => x.Date < DateTime.UtcNow).ToList();
        foreach (var expiredEvent in expiredEvents)
        {
            await eventRepository.Delete(expiredEvent.Id);
            // await requestRepository.DeleteAll(expiredEvent.Id);
        }
        
        cache.Remove(CacheKey);
    }

    public async Task DeleteAndCreateEventIndex()
    {
        await eventRepository.DeleteAndCreateEventIndex();
        cache.Remove(CacheKey);
    }

    public async Task<EventDetails?> Get(Guid id)
    {
        var cachedEvents = await GetCachedEventList();

        return cachedEvents.FirstOrDefault(x => x.Id == id);
    }

    public async Task<IList<EventDetails>> List(DateTime oldestDate, int size = 100)
    {
        var cachedEvents = await GetCachedEventList();

        return GetFilteredList(cachedEvents, oldestDate, size);
    }

    public void PurgeCache()
    {
        cache.Remove(CacheKey);
    }

    private async Task<IList<EventDetails>> GetCachedEventList()
    {
        if (cache.TryGetValue<IList<EventDetails>>(CacheKey, out var cachedEvents) && cachedEvents is { Count: > 0 })
        {
            return cachedEvents;
        }

        var events = await eventRepository.List();
        if (events is { Count: > 0 })
        {
            cache.Set(CacheKey, events, TimeSpan.FromHours(1));
        }

        return events;
    }

    private static List<EventDetails> GetFilteredList(IList<EventDetails>? events, DateTime oldestDate, int size = 100)
    {
        var eventsToFilter = events ?? [];

        return eventsToFilter
            .Where(x => x.Date >= oldestDate)
            .OrderBy(x => x.Date)
            .Take(size)
            .ToList();
    }
}