using System.Diagnostics.CodeAnalysis;

namespace DjPortalApi.Features.Requests;

public sealed class MusicRequestComparer : IEqualityComparer<MusicRequest>
{
    public bool Equals(MusicRequest? x, MusicRequest? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        return Guid.Equals(x.EventId, y.EventId) &&
               string.Equals(x.UserName, y.UserName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(x.TrackName, y.TrackName, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode([DisallowNull] MusicRequest obj)
    {
        return 0;
    }
}