namespace DjPortalApi.Features.Insights;

public sealed class TrackSearchTerm
{
    public string? Query { get; set; }

    public string? Environment { get; set; }

    public int UniqueCount { get; set; }

    public List<string> Variants { get; set; } = new();
}