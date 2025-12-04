using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace DjPortalApi.Features.Insights;

public sealed class InsightsService(IConfiguration configuration) : IInsightsService
{
    public async Task<IList<TrackSearchTerm>> GetSearchTerms(int numberOfDays = 7)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("customEvents");
        stringBuilder.AppendLine($"| where timestamp > ago({numberOfDays}d)");
        stringBuilder.AppendLine("| where tostring(customDimensions[\"Query\"]) != \"\"");
        stringBuilder.AppendLine("| where tostring(customDimensions[\"AspNetCoreEnvironment\"]) == \"Production\"");
        stringBuilder.AppendLine("| summarize UniqueCount=count() by Query=tolower(tostring(customDimensions[\"Query\"])), Environment=tostring(customDimensions[\"AspNetCoreEnvironment\"])");
        stringBuilder.AppendLine("| project Query, Environment, UniqueCount");
        stringBuilder.AppendLine("| order by UniqueCount desc");

        var query = stringBuilder.ToString();
        var settings = GetApplicationInsightSettings();

        // Build the request URL
        string url = $"https://api.applicationinsights.io/v1/apps/{settings.AppId}/query?query={Uri.EscapeDataString(query)}";

        using var client = new HttpClient();

        // Set up API key in the header
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("x-api-key", settings.ApiKey);

        // Send the request
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<InsightsResponse>(content);
            var searchTerms = GetSearchTerms(data).ToList();
            searchTerms = InsightsHelper.Deduplicate(searchTerms).ToList();

            return searchTerms;
        }

        return [];
    }

    private ApplicationInsightSettings GetApplicationInsightSettings()
    {
        var settings = new ApplicationInsightSettings();

        configuration.GetSection("ApplicationInsights").Bind(settings);

        return settings;
    }

    private static IEnumerable<TrackSearchTerm> GetSearchTerms(InsightsResponse? insightsResponse)
    {
        if (insightsResponse is not { Tables.Count: > 0 })
        {
            yield break;
        }

        foreach (var table in insightsResponse.Tables.Where(x => x is { Columns.Count: > 0 }))
        {
            var queryIndex = -1;
            var environmentIndex = -1;
            var uniqueCountIndex = -1;

            if (table is not { Columns.Count: > 0, Rows.Count: > 0 })
            {
                continue;
            }

            foreach (var column in table.Columns)
            {
                queryIndex = string.Equals(column.Name, "Query", StringComparison.OrdinalIgnoreCase) ? table.Columns.IndexOf(column) : queryIndex;
                environmentIndex = string.Equals(column.Name, "Environment", StringComparison.OrdinalIgnoreCase) ? table.Columns.IndexOf(column) : environmentIndex;
                uniqueCountIndex = string.Equals(column.Name, "UniqueCount", StringComparison.OrdinalIgnoreCase) ? table.Columns.IndexOf(column) : uniqueCountIndex;
            }

            foreach (var row in table.Rows)
            {
                yield return new TrackSearchTerm
                {
                    Query = queryIndex >= 0 ? row[queryIndex]?.ToString() : string.Empty,
                    Environment = environmentIndex >= 0 ? row[environmentIndex]?.ToString() : string.Empty,
                    UniqueCount = uniqueCountIndex >= 0 && int.TryParse(row[uniqueCountIndex]?.ToString(), out var uniqueCount) ? uniqueCount : 1
                };
            }
        }
    }
}