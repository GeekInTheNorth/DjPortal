using Azure.Search.Documents;
using DjPortalApi.Features.Common;
using Microsoft.Extensions.Configuration;

namespace DjPortalApi.Features.Tracks;

public sealed class TrackRepository(IConfiguration configuration) : BaseRepository(configuration), ITrackRepository
{
    public IList<Track> List(string? searchTerm)
    {
        return ListAsync(searchTerm).Result;
    }

    public async Task<IList<Track>> ListAsync(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || !TryCreateSearchClient(AppConstants.TrackIndexName, out var searchClient) || searchClient == null)
        {
            return [];
        }

        var response = await searchClient.SearchAsync<Track>(searchTerm.Trim(), new SearchOptions { Size = 10 });

        return  response.Value.GetResults().Select(x => x.Document).ToList();
    }
}