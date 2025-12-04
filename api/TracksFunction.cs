using DjPortalApi.Features;
using DjPortalApi.Features.Tracks;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DjPortalApi;

public class TracksFunction(ITrackRepository trackRepository, TelemetryClient telemetryClient) : BaseFunction
{
    [Function("TrackSearch")]
    public async Task<HttpResponseData> TrackSearch([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tracks/search")] HttpRequestData req)
    {
        var query = req.Query["query"];

        LogTrackSearch(query);

        var tracks = await trackRepository.ListAsync(query);

        return await CreateResponseAsync(req, System.Net.HttpStatusCode.OK, tracks);
    }

    private void LogTrackSearch(string? query)
    {
        if (query is not { Length: >3 })
        {
            return;
        }

        telemetryClient.TrackEvent("TrackSearch", new Dictionary<string, string> { { "Query", query } });
    }
}
