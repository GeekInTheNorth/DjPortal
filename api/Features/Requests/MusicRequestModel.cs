using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Requests;

public sealed class MusicRequestModel
{
    public string? EventId { get; set; }

    public string? MusicRequest { get; set; }

    public string? RequestedBy { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Bpm { get; set; }

    public string? Time { get; set; }

    public decimal SafeBpm => this.Bpm ?? 0;
}