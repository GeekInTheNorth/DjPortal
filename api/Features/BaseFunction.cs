using System.Net;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace DjPortalApi.Features;

public abstract class BaseFunction
{
    /// <summary>
    /// Gets the authenticated user from Azure Static Web Apps headers
    /// </summary>
    protected static StaticWebAppPrincipal GetAuthenticatedUser(HttpRequestData request)
    {
        var headerValue = request.Headers
            .FirstOrDefault(h => h.Key.Equals("x-ms-client-principal", StringComparison.OrdinalIgnoreCase))
            .Value?
            .FirstOrDefault();

        return StaticWebAppPrincipal.Parse(headerValue);
    }

    /// <summary>
    /// Gets the authenticated user from Azure Static Web Apps headers
    /// </summary>
    protected static StaticWebAppPrincipal GetAuthenticatedUser(HttpRequest request)
    {
        var headerValue = request.Headers
            .FirstOrDefault(h => h.Key.Equals("x-ms-client-principal", StringComparison.OrdinalIgnoreCase))
            .Value
            .FirstOrDefault();

        return StaticWebAppPrincipal.Parse(headerValue);
    }

    /// <summary>
    /// Checks if the user is authenticated and returns 401 response if not
    /// </summary>
    protected static HttpResponseData? RequireAuthentication(HttpRequestData request, out StaticWebAppPrincipal principal)
    {
        principal = GetAuthenticatedUser(request);
        
        if (!principal.IsAuthenticated)
        {
            return request.CreateResponse(HttpStatusCode.Unauthorized);
        }

        return null;
    }

    protected static bool GetUserCookieOrDefault(HttpRequestData request, out Guid userId)
    {
        userId = Guid.NewGuid();
        var cookie = request.Cookies.FirstOrDefault(x => AppConstants.RequestsIndexName.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase));
        if (!string.IsNullOrWhiteSpace(cookie?.Value) && Guid.TryParse(cookie?.Value, out var parsedUserId))
        {
            userId = parsedUserId;
            return true;
        }

        return false;
    }

    protected static async Task<HttpResponseData> CreateResponseAsync<TModel>(
        HttpRequestData request, 
        HttpStatusCode statusCode, 
        TModel model, 
        bool setUserCookie = false, 
        Guid? userId = null)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("X-Robots-Tag", "noindex, nofollow");

        if (setUserCookie && userId.HasValue)
        {
            response.Headers.Add("Set-Cookie", $"{AppConstants.RequestedByCookieName}={userId.Value}; HttpOnly; SameSite=Strict; Expires={DateTime.UtcNow.AddYears(1):R}; Path=/");
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var serializer = new JsonObjectSerializer(jsonOptions);

        await response.WriteAsJsonAsync(model, serializer);

        return response;
    }

    protected static HttpResponseData CreateEmptyResponse(
        HttpRequestData request, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        bool setUserCookie = false, 
        Guid? userId = null)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("X-Robots-Tag", "noindex, nofollow");

        if (setUserCookie && userId.HasValue)
        {
            response.Headers.Add("Set-Cookie", $"{AppConstants.RequestedByCookieName}={userId.Value}; HttpOnly; SameSite=Strict; Expires={DateTime.UtcNow.AddYears(1):R}; Path=/");
        }

        return response;
    }

    protected static async Task<TModel?> GetModelAsync<TModel>(HttpRequestData request)
    {
        try
        {
            return await request.ReadFromJsonAsync<TModel>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse request model", ex);
        }
    }

    protected static void AllowCors(HttpResponseData response)
    {
        // Allow CORS from any origin for this endpoint
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
    }
}