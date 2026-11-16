using System.ComponentModel.DataAnnotations;
using System.Net;
using DjPortalApi.Features;
using DjPortalApi.Features.Import;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DjPortalApi;

public class ImportFunction(
    IEventImportService eventImportService,
    ICerocClient cerocClient,
    ILogger<ImportFunction> logger) : BaseFunction
{
    [Function("GetCerocEvents")]
    public async Task<HttpResponseData> GetCerocEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "import/ceroc/list")] HttpRequestData req)
    {
        var authResponse = RequireAuthentication(req, out var _);
        if (authResponse != null) return authResponse;

        try
        {
            var listings = await eventImportService.GetListingsAsync(req.FunctionContext.CancellationToken);
            return await CreateResponseAsync(req, HttpStatusCode.OK, listings);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to read the Ceroc events listing.");
            return await CreateResponseAsync(req, HttpStatusCode.BadGateway, new { message = "Couldn't read the Ceroc events listing. Please try again later." });
        }
    }

    [Function("ImportCerocEvent")]
    public async Task<HttpResponseData> ImportCerocEvent([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/ceroc/event")] HttpRequestData req)
    {
        var authResponse = RequireAuthentication(req, out var _);
        if (authResponse != null) return authResponse;

        var model = await GetModelAsync<ImportEventModel>(req);
        model ??= new ImportEventModel();

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        if (!Validator.TryValidateObject(model, context, validationResults, true))
        {
            return await CreateResponseAsync(req, HttpStatusCode.BadRequest, validationResults);
        }

        // Only Ceroc pages are fetched, so this endpoint can't be used to make the server call anywhere else.
        if (!Uri.TryCreate(model.Url, UriKind.Absolute, out var eventUrl) || !cerocClient.IsCerocUrl(eventUrl))
        {
            return await CreateResponseAsync(req, HttpStatusCode.BadRequest, new { message = "Only https://www.ceroc.com event pages can be imported." });
        }

        try
        {
            var draft = await eventImportService.CreateDraftAsync(eventUrl, req.FunctionContext.CancellationToken);
            if (draft is null)
            {
                return await CreateResponseAsync(req, HttpStatusCode.BadGateway, new { message = "The AI couldn't summarise this event. You can still fill in the details by hand." });
            }

            return await CreateResponseAsync(req, HttpStatusCode.OK, draft);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to read the Ceroc event page {EventUrl}.", eventUrl);
            return await CreateResponseAsync(req, HttpStatusCode.BadGateway, new { message = "Couldn't read the Ceroc event page. Please try again later." });
        }
    }

    [Function("DescribeEvent")]
    public async Task<HttpResponseData> DescribeEvent([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/description")] HttpRequestData req)
    {
        var authResponse = RequireAuthentication(req, out var _);
        if (authResponse != null) return authResponse;

        var model = await GetModelAsync<DescribeEventModel>(req);
        model ??= new DescribeEventModel();

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        if (!Validator.TryValidateObject(model, context, validationResults, true))
        {
            return await CreateResponseAsync(req, HttpStatusCode.BadRequest, validationResults);
        }

        var description = await eventImportService.WriteDescriptionAsync(model, req.FunctionContext.CancellationToken);
        if (description is null)
        {
            return await CreateResponseAsync(req, HttpStatusCode.BadGateway, new { message = "The AI couldn't write a description just now. Please try again." });
        }

        return await CreateResponseAsync(req, HttpStatusCode.OK, new { description });
    }
}
