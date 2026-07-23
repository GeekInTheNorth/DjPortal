using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Requests;

public sealed class MusicRequestTrackUpdateModel
{
    public string? RequestId { get; set; }

    public string? TrackName { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Bpm { get; set; }

    public string? Time { get; set; }

    public decimal SafeBpm => this.Bpm ?? 0;
}
