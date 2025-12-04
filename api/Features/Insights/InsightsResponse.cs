using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Insights;

public sealed class InsightsResponse
{
    [JsonPropertyName("tables")]
    public List<InsightsTable>? Tables { get; set; }
}
