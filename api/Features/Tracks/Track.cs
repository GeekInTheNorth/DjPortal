using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Tracks;

public class Track
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("artist")]
    public string? Artist { get; set; } 

    [JsonPropertyName("album")]
    public string? Album { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("bpm")]
    public decimal BPM { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("summary")]
    public string Summary => $"{Title}, {Artist}";
}