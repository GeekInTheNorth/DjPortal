using System.Net;
using System.Text.RegularExpressions;
using DjPortalApi.Features;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace DjPortalApi;

public partial class NuGetDownloadsFunction : BaseFunction
{
    [Function("GetOptimizelyDownloads")]
    public async Task<HttpResponseData> GetOptimizelyDownloads(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route ="downloads/optimizely")] HttpRequestData req)
    {
        var packageName = req.Query["packageName"];
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return CreateEmptyResponse(req, HttpStatusCode.BadRequest);
        }
        var url = $"https://nuget.optimizely.com/packages/{packageName}/";

        using var http = new HttpClient();
        var html = await http.GetStringAsync(url);
        var match = DownloadsRegex().Match(html);
        var downloads = match.Success
            ? match.Groups[1].Value.Replace(",", "")
            : "0";

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.Headers.Add("Cache-Control", "public, max-age=86400");

        await response.WriteAsJsonAsync(new
        {
            schemaVersion = 1,
            label = "downloads",
            message = downloads,
            color = "blue"
        });

        return response;
    }

    [GeneratedRegex(@"([0-9,]+)\s*total\s*downloads", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-GB")]
    private static partial Regex DownloadsRegex();
}