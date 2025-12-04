namespace DjPortalApi.Features.Spotify;

public class SpotifySettings
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }
    
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
