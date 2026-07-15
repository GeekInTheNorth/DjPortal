using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Spotify;

public class SpotifyTrack
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("artists")]
    public List<SpotifyArtist>? Artists { get; set; }

    [JsonPropertyName("external_urls")]
    public SpotifyExternalUrls? ExternalUrls { get; set; }

    [JsonIgnore]
    public string? ExternalUrl => ExternalUrls?.Spotify;
}

public class SpotifyExternalUrls
{
    [JsonPropertyName("spotify")]
    public string? Spotify { get; set; }
}
