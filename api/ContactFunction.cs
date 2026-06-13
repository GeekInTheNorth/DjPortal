using DjPortalApi.Features;
using DjPortalApi.Features.Contact;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;

namespace DjPortalApi;

public class ContactFunction(
    IEmailService emailService,
    IMemoryCache memoryCache) : BaseFunction
{
    private const int MaxSubmissionsPerWindow = 3;
    private const int MinFormFillMs = 3000;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(10);

    [Function("SubmitContact")]
    public async Task<HttpResponseData> SubmitContact([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact/submit")] HttpRequestData req)
    {
        var model = await GetModelAsync<ContactModel>(req);
        if (model == null)
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.BadRequest);
        }

        // Honeypot: real users never see the Company field. Drop silently so bots get no signal.
        if (!string.IsNullOrWhiteSpace(model.Company))
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.OK);
        }

        // Timing: submissions faster than a human could fill the form are automated. Drop silently.
        if (model.ElapsedMs < MinFormFillMs)
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.OK);
        }

        if (string.IsNullOrWhiteSpace(model.Name) ||
            string.IsNullOrWhiteSpace(model.Subject) ||
            string.IsNullOrWhiteSpace(model.Message) ||
            !MailboxAddress.TryParse(model.Email, out _))
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.BadRequest);
        }

        // Best-effort per-IP rate limit. In-memory, so per-instance only - a soft deterrent.
        var cacheKey = $"contact-rate:{GetClientIp(req)}";
        var count = memoryCache.TryGetValue(cacheKey, out int existing) ? existing : 0;
        if (count >= MaxSubmissionsPerWindow)
        {
            return CreateEmptyResponse(req, System.Net.HttpStatusCode.TooManyRequests);
        }

        memoryCache.Set(cacheKey, count + 1, RateLimitWindow);

        await emailService.SendContactEmailAsync(model);

        return CreateEmptyResponse(req, System.Net.HttpStatusCode.OK);
    }

    private static string GetClientIp(HttpRequestData req)
    {
        var header = req.Headers
            .FirstOrDefault(h => h.Key.Equals("X-Forwarded-For", StringComparison.OrdinalIgnoreCase))
            .Value?
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(header))
        {
            return "unknown";
        }

        // X-Forwarded-For is a comma-separated list; the original client is first.
        // Azure may append a :port, so strip anything after the last colon if present.
        var first = header.Split(',')[0].Trim();
        var lastColon = first.LastIndexOf(':');
        return lastColon > 0 && !first.Contains("::") ? first[..lastColon] : first;
    }
}
