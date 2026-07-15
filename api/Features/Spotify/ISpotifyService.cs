using System.Diagnostics.CodeAnalysis;

namespace DjPortalApi.Features.Spotify;

public interface ISpotifyService
{
    Task<SpotifyTrack?> GetTrack(string trackId);

    Task<IList<SpotifyTrack>> SearchTracks(string query, int limit = 5);

    bool TryGetSpotifyId(string? url, [NotNullWhen(true)] out string? id);
}
