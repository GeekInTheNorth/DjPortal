using System.Text.Json.Serialization;

namespace DjPortalApi.Features.WebSearch;

public sealed class WebSearchResult
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
