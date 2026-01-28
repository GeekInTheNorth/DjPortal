using System.Globalization;
using System.Net;
using CsvHelper;
using CsvHelper.Configuration;
using DjPortalApi.Features;
using DjPortalApi.Features.Tracks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        var response = await CreateResponseAsync(req, System.Net.HttpStatusCode.OK, tracks);

        AllowCors(response);

        return response;
    }

    [Function("TrackCsvUpload")]
    public async Task<IActionResult> TrackCsvUpload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tracks/csvupload")] HttpRequest req)
    {
        var user = GetAuthenticatedUser(req);
        if (!user.IsAuthenticated)
        {
            return new UnauthorizedResult();
        }

        try
        {
            // Parse the multipart form data
            var formData = await req.ReadFormAsync();
            if (formData is not { Files.Count: > 0 })
            {
                throw new Exception("No files uploaded");
            }

            var csvConfig = new CsvConfiguration
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                CultureInfo = CultureInfo.InvariantCulture,
            };
            var file = formData.Files[0];
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, csvConfig);
            {
                csv.Configuration.RegisterClassMap<TrackMap>();
                var records = csv.GetRecords<Track>().ToList();
                await trackRepository.DeleteAndCreateTrackIndexAsync(records);
            }

            return new OkObjectResult(new { Message = "File processed successfully" });
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { Error = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
        }
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
