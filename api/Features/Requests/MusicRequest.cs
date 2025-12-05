using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Requests;

public sealed class MusicRequest
{
    private string? _status;

    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("trackName")]
    public string? TrackName { get; set; }

    [JsonPropertyName("spotifyUrl")]
    public string? SpotifyUrl { get; set; }

    [JsonPropertyName("status")]
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
        _ => 4
    };
}
