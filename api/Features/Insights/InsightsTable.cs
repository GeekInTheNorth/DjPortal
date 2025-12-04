using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Insights;

public sealed class InsightsTable
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("columns")]
    public List<InsightsColumn>? Columns { get; set; }

    [JsonPropertyName("rows")]
    public List<List<object>>? Rows { get; set; }
}
