using DjPortalApi.Features;
using DjPortalApi.Features.Insights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DjPortalApi;

public class InsightsFunction(IInsightsService insightsService) : BaseFunction
{
    [Function("GetSearchTerms")]
    public async Task<HttpResponseData> GetSearchTerms([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "insights/searchterms")] HttpRequestData req)
    {
        var defaultDays = 7;
        var parseSuccess = int.TryParse(req.Query["numberOfDays"], out var numberOfDays);
        var searchTerms = await insightsService.GetSearchTerms(parseSuccess ? numberOfDays : defaultDays);

        return await CreateResponseAsync(req, System.Net.HttpStatusCode.OK, searchTerms);
    }
}