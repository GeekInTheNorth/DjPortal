namespace DjPortalApi.Features.Requests;

public sealed class MusicRequestModel
{
    public string? EventId { get; set; }

    public string? MusicRequest { get; set; }

    public string? RequestedBy { get; set; }

    public string? Bpm { get; set; }

    public string? Time { get; set; }

    public decimal SafeBpm => decimal.TryParse(this.Bpm, out var bpm) ? bpm : 0;
}