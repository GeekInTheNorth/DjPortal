namespace DjPortalApi.Features.Tracks;

public interface ITrackRepository
{
    IList<Track> List(string? searchTerm, decimal lowBpm, decimal highBpm);

    Task<IList<Track>> ListAsync(string? searchTerm, decimal lowBpm, decimal highBpm);

    Task DeleteAndCreateTrackIndexAsync(IList<Track> items);
}
