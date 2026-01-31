using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using DjPortalApi.Features.Common;
using Microsoft.Extensions.Configuration;

namespace DjPortalApi.Features.Requests;

public sealed class RequestRepository(IConfiguration configuration) : BaseRepository(configuration), IRequestRepository
{
    public async Task<IList<MusicRequest>> Get(Guid eventId)
    {
        if (!TryCreateSearchClient(AppConstants.RequestsIndexName, out var searchClient))
        {
            return [];
        }

        try
        {
            var response = await searchClient.SearchAsync<MusicRequest>(new SearchOptions { Size = 100, Filter = $"{nameof(MusicRequest.EventId)} eq '{eventId}'" });

            return response.Value.GetResults().Select(x => x.Document).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task Delete(Guid requestId)
    {
        if (!TryCreateSearchClient(AppConstants.RequestsIndexName, out var searchClient))
        {
            return;
        }

        await searchClient.DeleteDocumentsAsync("Id", [requestId.ToString()]);
    }

    public async Task DeleteAll(Guid eventId)
    {
        if (!TryCreateSearchClient(AppConstants.RequestsIndexName, out var searchClient))
        {
            return;
        }

        var response = await searchClient.SearchAsync<MusicRequest>(new SearchOptions { Size = 100, Filter = $"{nameof(MusicRequest.EventId)} eq '{eventId}'" });
        var musicRequests = response.Value.GetResults().Select(x => x.Document).ToList();

        await searchClient.DeleteDocumentsAsync(musicRequests.Select(x => new { x.Id }));
    }

    public async Task Add(MusicRequest request)
    {
        if (!TryCreateSearchClient(AppConstants.RequestsIndexName, out var searchClient))
        {
            return;
        }

        await searchClient.UploadDocumentsAsync([request]);
    }

    public async Task UpdateStatus(Guid requestId, RequestStatus status)
    {
        if (!TryCreateSearchClient(AppConstants.RequestsIndexName, out var searchClient))
        {
            return;
        }
        
        var musicRequest = await searchClient.GetDocumentAsync<MusicRequest>(requestId.ToString());
        if (musicRequest?.Value == null)
        {
            return;
        }

        musicRequest.Value.Status = status.ToString();
        await searchClient.UploadDocumentsAsync([musicRequest.Value]);
    }

    public async Task DeleteAndCreateRequestIndex()
    {
        if (!TryCreateSearchIndexClient(out var searchIndexClient))
        {
            return;
        }

        await searchIndexClient.DeleteIndexAsync(AppConstants.RequestsIndexName);

        var idField = new SimpleField(nameof(MusicRequest.Id), SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true };
        var eventIdField = new SimpleField(nameof(MusicRequest.EventId), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true };
        var userIdField = new SimpleField(nameof(MusicRequest.UserId), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true };
        var userNameField = new SearchableField(nameof(MusicRequest.UserName));
        var trackNameField = new SearchableField(nameof(MusicRequest.TrackName));
        var statusField = new SimpleField(nameof(MusicRequest.Status), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true };
        var spotifyUrlField = new SimpleField(nameof(MusicRequest.SpotifyUrl), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true };

        await searchIndexClient.CreateIndexAsync(new SearchIndex(AppConstants.RequestsIndexName)
        {
            Fields = { idField, eventIdField, userIdField, userNameField, trackNameField, statusField, spotifyUrlField }
        });
    }
}
