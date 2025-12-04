using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Spotify;

public class SpotifyArtist
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
