using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DjPortalApi.Features.Extensions;

namespace DjPortalApi.Features.Events;

public sealed class EventDetails : IEventDetailsData
{
    private DateTime? _startTime;

    private DateTime? _endTime;

    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTime Date { get; set; }

    public DateTime StartTime 
    { 
        get
        {
            TimesToDates();
            return _startTime ?? Date;
        }
    }

    public DateTime EndTime 
    { 
        get
        {
            TimesToDates();
            return _endTime ?? Date;
        }
    }

    public string? Times { get; set; }

    public string? LocationName { get; set; }

    public string? LocationAddress { get; set; }

    [JsonConverter(typeof(NullableBooleanJsonConverter))]
    public bool IsRequestable { get; set; }

    [JsonConverter(typeof(NullableBooleanJsonConverter))]
    public bool IsCancelled { get; set; }

    [JsonConverter(typeof(NullableBooleanJsonConverter))]
    public bool GenerateSchemaData { get; set; }

    public string CalendarInviteUrl => $"/api/events/getinvite/{Id}/dance-event.ics";

    public string? FacebookEventUrl => string.IsNullOrWhiteSpace(FacebookEventId) ? null : $"https://www.facebook.com/events/{FacebookEventId}";

    public string? FacebookEventId { get; set; }

    [MemberNotNullWhen(true, nameof(FacebookEventId), nameof(FacebookEventUrl))]
    public bool IsFacebookEvent => !string.IsNullOrWhiteSpace(FacebookEventId);

    public string? Tags { get; set; }

    public IEnumerable<EventDetailTag> TagList => GetTags();

    private void TimesToDates()
    {
        if (string.IsNullOrWhiteSpace(Times) || _startTime.HasValue || _endTime.HasValue)
        {
            return;
        }

        try
        {
            // 20:00 - 23:00
            var startHour = int.Parse(Times.Substring(0, 2));
            var startMinute = int.Parse(Times.Substring(3, 2));
            var endHour = int.Parse(Times.Substring(8, 2));
            var endMinute = int.Parse(Times.Substring(11, 2));

            _startTime = new DateTime(Date.Year, Date.Month, Date.Day, startHour, startMinute, 0);
            _endTime = new DateTime(Date.Year, Date.Month, Date.Day, endHour, endMinute, 0);

            if (_endTime.Value < _startTime.Value)
            {
                _endTime = _endTime.Value.AddDays(1);
            }
        }
        catch (Exception)
        {
            _startTime = new DateTime(Date.Year, Date.Month, Date.Day, 20, 00, 0);
            _endTime = new DateTime(Date.Year, Date.Month, Date.Day, 23, 30, 0);
        }
    }

    private IEnumerable<EventDetailTag> GetTags()
    {
        var tags = Tags.SplitByComma();
        foreach (var tag in tags)
        {
            yield return new EventDetailTag(tag, GetTagColour(tag));
        }   
    }

    private static string GetTagColour(string tag)
    {
        if (tag.Equals("DJ Mark", StringComparison.InvariantCultureIgnoreCase))
        {
            return "success";
        }

        if (tag.StartsWith("Valentine", StringComparison.InvariantCultureIgnoreCase)
         || tag.EndsWith("Ball", StringComparison.InvariantCultureIgnoreCase))
        {
            return "danger";
        }

        return "primary";
    }
}