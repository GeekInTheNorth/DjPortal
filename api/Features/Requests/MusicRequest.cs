using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Requests;

public sealed class MusicRequest
{
    private string? _status;

    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid UserId { get; set; }

    public string? UserName { get; set; }

    public string? TrackName { get; set; }

    public string? SpotifyUrl { get; set; }

    public string? Status
    {
        get => string.IsNullOrWhiteSpace(_status) ? RequestStatus.Pending.ToString() : _status;
        set => _status = value;
    }

    [JsonIgnore]
    public int StatusOrder => _status switch
    {
        "Approved" => 0,
        "Queued" => 1,
        "Pending" => 2,
        "Played" => 3,
        "AlreadyServed" => 4,
        _ => 5
    };
}
