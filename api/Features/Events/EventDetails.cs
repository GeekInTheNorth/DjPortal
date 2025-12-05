using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DjPortalApi.Features.Extensions;

namespace DjPortalApi.Features.Events;

public sealed class EventDetails : IEventDetailsData
{
    private DateTime? _startTime;

    private DateTime? _endTime;

    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("startTime")]
    public DateTime StartTime 
    { 
        get
        {
            TimesToDates();
            return _startTime ?? Date;
        }
    }

    [JsonPropertyName("endTime")]
    public DateTime EndTime 
    { 
        get
        {
            TimesToDates();
            return _endTime ?? Date;
        }
    }

    [JsonPropertyName("times")]
    public string? Times { get; set; }

    [JsonPropertyName("locationName")]
    public string? LocationName { get; set; }

    [JsonPropertyName("locationAddress")]
    public string? LocationAddress { get; set; }

    [JsonPropertyName("isRequestable")]
    [JsonConverter(typeof(NullableBooleanJsonConverter))]
    public bool IsRequestable { get; set; }

    [JsonPropertyName("isCancelled")]
    [JsonConverter(typeof(NullableBooleanJsonConverter))]
    public bool IsCancelled { get; set; }

    [JsonPropertyName("generateSchemaData")]
    [JsonConverter(typeof(NullableBooleanJsonConverter))]
    public bool GenerateSchemaData { get; set; }

    [JsonPropertyName("calendarInviteUrl")]
    public string CalendarInviteUrl => $"/events/getinvite/{Id}/dance-event.ics";

    [JsonPropertyName("facebookEventUrl")]
    public string? FacebookEventUrl => string.IsNullOrWhiteSpace(FacebookEventId) ? null : $"https://www.facebook.com/events/{FacebookEventId}";

    [JsonPropertyName("facebookEventId")]
    public string? FacebookEventId { get; set; }

    [JsonPropertyName("isFacebookEvent")]
    [MemberNotNullWhen(true, nameof(FacebookEventId), nameof(FacebookEventUrl))]
    public bool IsFacebookEvent => !string.IsNullOrWhiteSpace(FacebookEventId);

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    [JsonPropertyName("tagList")]
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