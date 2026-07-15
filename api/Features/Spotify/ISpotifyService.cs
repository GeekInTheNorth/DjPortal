using System.Diagnostics.CodeAnalysis;

namespace DjPortalApi.Features.Spotify;

public interface ISpotifyService
{
    Task<SpotifyTrack?> GetTrack(string trackId);

    bool TryGetSpotifyId(string? url, [NotNullWhen(true)] out string? id);
}
