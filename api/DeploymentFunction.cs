using System.Net;
using DjPortalApi.Features;
using DjPortalApi.Features.Deployment;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DjPortalApi;

public class DeploymentFunction(IDeploymentService deploymentService, ILogger<DeploymentFunction> logger) : BaseFunction
{
    [Function("TriggerRebuild")]
    public async Task<HttpResponseData> TriggerRebuild(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "deployment/rebuild")]
        HttpRequestData req)
    {
        // Authentication check
        var authResponse = RequireAuthentication(req, out var principal);
        if (authResponse != null) return authResponse;

        logger.LogInformation("Rebuild triggered by user: {UserId}", principal.UserId);

        try
        {
            var result = await deploymentService.TriggerRebuild();

            if (result.Success)
            {
                logger.LogInformation("Rebuild triggered successfully for user: {UserId}", principal.UserId);
                return await CreateResponseAsync(req, HttpStatusCode.OK, new { message = result.Message });
            }

            logger.LogWarning("Rebuild trigger failed for user: {UserId}. Status: {StatusCode}, Message: {Message}",
                principal.UserId, result.StatusCode, result.Message);

            var statusCode = result.StatusCode switch
            {
                401 => HttpStatusCode.Unauthorized,
                404 => HttpStatusCode.NotFound,
                422 => HttpStatusCode.UnprocessableEntity,
                429 => HttpStatusCode.TooManyRequests,
                _ => HttpStatusCode.BadGateway
            };

            return await CreateResponseAsync(req, statusCode, new { error = result.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while triggering rebuild for user: {UserId}", principal.UserId);
            return await CreateResponseAsync(req, HttpStatusCode.InternalServerError,
                new { error = "An error occurred while triggering the rebuild. Please try again later." });
        }
    }
}
