namespace DjPortalApi.Features.Tracks;

public interface ITrackRepository
{
    IList<Track> List(string? searchTerm);

    Task<IList<Track>> ListAsync(string? searchTerm);

    Task DeleteAndCreateTrackIndexAsync(IList<Track> items);
}
