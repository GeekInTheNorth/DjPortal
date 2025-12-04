using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using DjPortalApi.Features.Common;
using Microsoft.Extensions.Configuration;

namespace DjPortalApi.Features.Events;

public sealed class EventRepository(IConfiguration configuration) : BaseRepository(configuration), IEventRepository
{
    public async Task Create(CreateEventModel eventDetails)
    {
        if (!TryCreateSearchClient(AppConstants.EventIndexName, out var searchClient))
        {
            return;
        }

        await searchClient.UploadDocumentsAsync([new {
            Id = Guid.NewGuid(),
            eventDetails.Name,
            eventDetails.Description,
            eventDetails.Date,
            eventDetails.Times,
            eventDetails.LocationName,
            eventDetails.LocationAddress,
            eventDetails.FacebookEventId,
            eventDetails.Tags,
            eventDetails.IsRequestable,
            eventDetails.GenerateSchemaData
        }]);
    }

    public async Task Update(UpdateEventModel eventDetails)
    {
        if (!TryCreateSearchClient(AppConstants.EventIndexName, out var searchClient) ||
            !Guid.TryParse(eventDetails.Id, out var eventId))
        {
            return;
        }

        await searchClient.MergeOrUploadDocumentsAsync([new {
            Id = eventId,
            eventDetails.Name,
            eventDetails.Description,
            eventDetails.Date,
            eventDetails.Times,
            eventDetails.LocationName,
            eventDetails.LocationAddress,
            eventDetails.FacebookEventId,
            eventDetails.Tags,
            eventDetails.IsRequestable,
            eventDetails.GenerateSchemaData,
            eventDetails.IsCancelled
        }]);
    }

    public async Task Delete(Guid id)
    {
        if (!TryCreateSearchClient(AppConstants.EventIndexName, out var searchClient))
        {
            return;
        }

        await searchClient.DeleteDocumentsAsync([new { Id = id.ToString() }]);
    }

    public async Task<IList<EventDetails>> List()
    {
        if (!TryCreateSearchClient(AppConstants.EventIndexName, out var searchClient))
        {
            return [];
        }

        var searchOptions = new SearchOptions
        { 
            OrderBy = { $"{nameof(EventDetails.Date)} asc" },
            Size = 100
        };

        var searchRequest = await searchClient.SearchAsync<EventDetails>(string.Empty, searchOptions);

        var searchResults = searchRequest.Value.GetResults().Select(x => x.Document).ToList();; 
        if (searchResults is { Count: >0 })
        {
            return searchResults;
        }

        return [];
    }

    public async Task DeleteAndCreateEventIndex()
    {
        if (!TryCreateSearchIndexClient(out var searchIndexClient))
        {
            return;
        }

        await searchIndexClient.DeleteIndexAsync(AppConstants.EventIndexName);

        var idField = new SimpleField(nameof(EventDetails.Id), SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true };

        var eventNameField = new SearchableField(nameof(EventDetails.Name))
        {
            IsFilterable = true,
            IsSortable = true,
            AnalyzerName = LexicalAnalyzerName.StandardAsciiFoldingLucene
        };

        var descriptionField = new SearchableField(nameof(EventDetails.Description))
        {
            IsFilterable = true,
            IsSortable = true,
            AnalyzerName = LexicalAnalyzerName.StandardAsciiFoldingLucene
        };

        var eventDateField = new SimpleField(nameof(EventDetails.Date), SearchFieldDataType.DateTimeOffset)
        { 
            IsFilterable = true, 
            IsSortable = true
        };

        var timesField = new SearchableField(nameof(EventDetails.Times))
        {
            IsFilterable = true,
            IsSortable = true,
            AnalyzerName = LexicalAnalyzerName.StandardAsciiFoldingLucene
        };

        var locationNameField = new SearchableField(nameof(EventDetails.LocationName))
        {
            IsFilterable = true,
            IsSortable = true,
            AnalyzerName = LexicalAnalyzerName.StandardAsciiFoldingLucene
        };

        var locationAddressField = new SearchableField(nameof(EventDetails.LocationAddress))
        {
            IsFilterable = true,
            IsSortable = true,
            AnalyzerName = LexicalAnalyzerName.StandardAsciiFoldingLucene
        };

        var facebookEventIdField = new SimpleField(nameof(EventDetails.FacebookEventId), SearchFieldDataType.String)
        { 
            IsFilterable = true, 
            IsSortable = true
        };

        var tagsField = new SimpleField(nameof(EventDetails.Tags), SearchFieldDataType.String)
        { 
            IsFilterable = true, 
            IsSortable = true
        };

        var isRequestableField = new SimpleField(nameof(EventDetails.IsRequestable), SearchFieldDataType.Boolean)
        { 
            IsFilterable = true, 
            IsSortable = false
        };

        var generateSchemaDataField = new SimpleField(nameof(EventDetails.GenerateSchemaData), SearchFieldDataType.Boolean)
        { 
            IsFilterable = true, 
            IsSortable = false
        };

        var isCancelledField = new SimpleField(nameof(EventDetails.IsCancelled), SearchFieldDataType.Boolean)
        { 
            IsFilterable = true, 
            IsSortable = false
        };

        await searchIndexClient.CreateIndexAsync(new SearchIndex(AppConstants.EventIndexName)
        {
            Fields = { 
                idField, 
                eventNameField, 
                descriptionField, 
                timesField, 
                eventDateField, 
                locationNameField, 
                locationAddressField, 
                facebookEventIdField, 
                tagsField,
                isRequestableField,
                generateSchemaDataField,
                isCancelledField
            }
        });
    }
}
