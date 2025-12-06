using Azure.Search.Documents.Indexes;

namespace DjPortalApi.Features.Tracks;

public class Track
{
    [SearchableField(IsKey = true)]
    public string? Id { get; set; }
    
    public string? Title { get; set; }

    public string? Artist { get; set; } 

    public string? Album { get; set; }

    public string? Time { get; set; }

    public decimal BPM { get; set; }

    public string? Key { get; set; }

    public string Summary => $"{Title}, {Artist}";
}