using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Insights;

public sealed class TrackSearchTerm
{
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("environment")]
    public string? Environment { get; set; }

    [JsonPropertyName("uniqueCount")]
    public int UniqueCount { get; set; }

    [JsonPropertyName("variants")]
    public List<string> Variants { get; set; } = new();
}