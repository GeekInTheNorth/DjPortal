using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Insights;

public sealed class InsightsColumn
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
