using System.Net;
using System.Security.Cryptography;
using System.Text;
using DjPortalApi.Features;
using DjPortalApi.Features.Mcp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DjPortalApi;

/// <summary>
/// The public MCP server (Streamable HTTP) at /api/mcp, and its Server Card at /api/mcp/server-card.
/// Like the REST API it is anonymous, and callers are always treated as visitors, never as the DJ.
/// </summary>
public class McpFunction(IMcpServer mcpServer, ILogger<McpFunction> logger) : BaseFunction
{
    // JSON-RPC messages to this server are small, so anything bigger is not from a real client.
    private const int MaxBodyLength = 64 * 1024;

    private const string ProductionHost = "dj.stott.pro";

    private static readonly string ServerCardJson = McpServerInfo.ServerCard().ToJsonString(McpJson.Options);

    private static readonly string ServerCardETag =
        $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ServerCardJson)))[..16].ToLowerInvariant()}\"";

    [Function("Mcp")]
    public async Task<HttpResponseData> Mcp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "delete", Route = "mcp")] HttpRequestData req)
    {
        // Stops a web page from driving the server through DNS rebinding. Agents and desktop clients send no
        // Origin header, so they are unaffected.
        if (!IsAllowedOrigin(req))
        {
            return await WriteAsync(req, McpHttpResult.Error(HttpStatusCode.Forbidden, null, JsonRpcErrorCodes.InvalidRequest, "Origin not allowed."));
        }

        // The current transport has no GET event stream and no sessions to DELETE, and this server issues no
        // sessions to clients on older versions either.
        if (!string.Equals(req.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            var notAllowed = req.CreateResponse(HttpStatusCode.MethodNotAllowed);
            notAllowed.Headers.Add("Allow", "POST");
            return notAllowed;
        }

        var body = await ReadBodyAsync(req);
        if (body is null)
        {
            return await WriteAsync(req, McpHttpResult.Error(HttpStatusCode.RequestEntityTooLarge, null, JsonRpcErrorCodes.InvalidRequest, "The request is too large."));
        }

        McpHttpResult result;
        try
        {
            var headers = new McpRequestHeaders(
                GetHeader(req, "MCP-Protocol-Version"),
                GetHeader(req, "Mcp-Method"),
                GetHeader(req, "Mcp-Name"));

            result = await mcpServer.HandleAsync(body, headers);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MCP request failed.");
            result = McpHttpResult.Error(HttpStatusCode.InternalServerError, null, JsonRpcErrorCodes.InternalError, "Internal error.");
        }

        return await WriteAsync(req, result);
    }

    /// <summary>
    /// The Server Card, at the location the MCP Server Card extension reserves: the endpoint URL plus
    /// /server-card. The AI catalogue links here.
    /// </summary>
    [Function("McpServerCard")]
    public async Task<HttpResponseData> McpServerCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "mcp/server-card")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        // The card is public, read-only metadata, so any site may read it.
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, If-None-Match");
        response.Headers.Add("Access-Control-Expose-Headers", "ETag");

        if (string.Equals(req.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = HttpStatusCode.NoContent;
            return response;
        }

        response.Headers.Add("Cache-Control", "public, max-age=3600");
        response.Headers.Add("ETag", ServerCardETag);

        var ifNoneMatch = GetHeader(req, "If-None-Match");
        if (ifNoneMatch is not null && ifNoneMatch.Split(',').Select(x => x.Trim()).Any(x => x == "*" || x == ServerCardETag || x == $"W/{ServerCardETag}"))
        {
            response.StatusCode = HttpStatusCode.NotModified;
            return response;
        }

        response.Headers.Add("Content-Type", "application/mcp-server-card+json");
        await response.WriteStringAsync(ServerCardJson);

        return response;
    }

    // Allows the live site, local development and whatever host the request came in on (such as a preview
    // environment). A browser sends "null" or an unparseable origin from sandboxed pages, which is refused.
    private static bool IsAllowedOrigin(HttpRequestData req)
    {
        var origin = GetHeader(req, "Origin");
        if (origin is null)
        {
            return true;
        }

        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.IsLoopback
                || uri.Host.Equals(ProductionHost, StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals(req.Url.Host, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string?> ReadBodyAsync(HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body, Encoding.UTF8);
        var buffer = new char[MaxBodyLength + 1];
        var length = await reader.ReadBlockAsync(buffer, 0, buffer.Length);

        return length > MaxBodyLength ? null : new string(buffer, 0, length);
    }

    private static async Task<HttpResponseData> WriteAsync(HttpRequestData req, McpHttpResult result)
    {
        var response = req.CreateResponse(result.StatusCode);
        response.Headers.Add("X-Robots-Tag", "noindex, nofollow");

        if (result.Body is not null)
        {
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(result.Body.ToJsonString(McpJson.Options));
        }

        return response;
    }

    private static string? GetHeader(HttpRequestData req, string name)
        => req.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
}
