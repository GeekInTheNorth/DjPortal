using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
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

    public async Task DeleteAndCreateTrackIndexAsync(IList<Track> items)
    {
        if (items is not { Count: > 0 } || !TryCreateSearchIndexClient(out var searchIndexClient))
        {
            return;
        }

        await searchIndexClient.DeleteIndexAsync(AppConstants.TrackIndexName);

        var synonyms = new List<string>
        {
            "pink,P!nk",
            "&,and",
            "B*witched,bewitched",
            "B-52s,B52s,B-52,B52",
            "Rag'N'Bone,ragnbone,Rag and bone,rag & bone,rag n bone",
        };
        var synonymList = string.Join("\n", synonyms);
        var synonymMap = new SynonymMap(AppConstants.SynonymMapName, synonymList);
        await searchIndexClient.CreateOrUpdateSynonymMapAsync(synonymMap);

        var idField = new SimpleField(nameof(Track.Id), SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true };

        var titleField = new SearchableField(nameof(Track.Title));
        titleField.SynonymMapNames.Add(AppConstants.SynonymMapName);
        titleField.IsFilterable = true;
        titleField.IsSortable = true;
        titleField.AnalyzerName = LexicalAnalyzerName.StandardAsciiFoldingLucene;

        var artistField = new SearchableField(nameof(Track.Artist));
        artistField.SynonymMapNames.Add(AppConstants.SynonymMapName);
        artistField.IsFilterable = true;
        artistField.IsSortable = true;
        artistField.AnalyzerName = LexicalAnalyzerName.StandardAsciiFoldingLucene;

        var albumField = new SimpleField(nameof(Track.Album), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true };
        var timeField = new SimpleField(nameof(Track.Time), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true };
        var bpmField = new SimpleField(nameof(Track.BPM), SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true };
        var keyField = new SimpleField(nameof(Track.Key), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true };

        await searchIndexClient.CreateIndexAsync(new SearchIndex(AppConstants.TrackIndexName)
        {
            Fields = { idField, titleField, artistField, albumField, timeField, bpmField, keyField }
        });

        if (TryCreateSearchClient(AppConstants.TrackIndexName, out var searchClient))
        {
            var batch = IndexDocumentsBatch.Upload(items);
            await searchClient.IndexDocumentsAsync(batch);
        }
    }
}