using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Configuration;

namespace DjPortalApi.Features.Common;

public abstract class BaseRepository(IConfiguration configuration)
{
    protected bool TryCreateSearchClient(string indexName, [NotNullWhen(true)] out SearchClient? searchClient)
    {
        if (string.IsNullOrWhiteSpace(indexName))
        {
            searchClient = null;
            return false;
        }

        var searchServiceUri = GetConfigValue("SearchServiceUri");
        var searchServiceApiKey = GetConfigValue("SearchServiceAdminApiKey");
        
        if (string.IsNullOrWhiteSpace(searchServiceUri) || string.IsNullOrWhiteSpace(searchServiceApiKey))
        {
            searchClient = null;
            return false;
        }

        searchClient = new SearchClient(new Uri(searchServiceUri), indexName, new AzureKeyCredential(searchServiceApiKey));
        return true;
    }

    protected bool TryCreateSearchIndexClient([NotNullWhen(true)] out SearchIndexClient? searchIndexClient)
    {
        var searchServiceUri = GetConfigValue("SearchServiceUri");
        var searchServiceApiKey = GetConfigValue("SearchServiceAdminApiKey");

        if (string.IsNullOrWhiteSpace(searchServiceUri) || string.IsNullOrWhiteSpace(searchServiceApiKey))
        {
            searchIndexClient = null;
            return false;
        }

        searchIndexClient = new SearchIndexClient(new Uri(searchServiceUri), new AzureKeyCredential(searchServiceApiKey));
        return true;
    }

    private string? GetConfigValue(string key)
    {
        return configuration.GetValue<string>(key);
    }
}