using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace DjPortalApi.Features.WebSearch;

public sealed class WebSearchService(HttpClient httpClient, IConfiguration configuration) : IWebSearchService
{
    private readonly string? _apiKey = configuration.GetValue<string>("TavilyApiKey");

    public async Task<IList<WebSearchResult>> Search(string query, int maxResults = 5)
    {
        // Degrade gracefully when no key is configured, so the assistant still works without web search.
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<WebSearchResult>();
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/search");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = JsonContent.Create(new
            {
                query,
                max_results = maxResults,
                search_depth = "basic"
            });

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<WebSearchResult>();
            }

            var responseStream = await response.Content.ReadAsStreamAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = await JsonSerializer.DeserializeAsync<TavilyResponse>(responseStream, options);
            return result?.Results ?? new List<WebSearchResult>();
        }
        catch
        {
            return Array.Empty<WebSearchResult>();
        }
    }

    private sealed class TavilyResponse
    {
        [JsonPropertyName("results")]
        public List<WebSearchResult>? Results { get; set; }
    }
}
