using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DjPortalApi.Features.Mcp;

/// <summary>The MCP headers that mirror fields of the request body.</summary>
public sealed record McpRequestHeaders(string? ProtocolVersion, string? Method, string? Name);

/// <summary>What to send back for one POST: a status code and, unless it is a 202, a JSON-RPC message.</summary>
public sealed record McpHttpResult(HttpStatusCode StatusCode, JsonObject? Body)
{
    public static McpHttpResult Accepted { get; } = new(HttpStatusCode.Accepted, null);

    /// <summary>A JSON-RPC error. The id is left out when the request's id could not be read.</summary>
    public static McpHttpResult Error(HttpStatusCode statusCode, JsonNode? id, int code, string message, JsonObject? data = null)
    {
        var error = new JsonObject { ["code"] = code, ["message"] = message };
        if (data is not null)
        {
            error["data"] = data;
        }

        var body = new JsonObject { ["jsonrpc"] = "2.0" };
        if (id is not null)
        {
            body["id"] = id.DeepClone();
        }

        body["error"] = error;

        return new McpHttpResult(statusCode, body);
    }
}

/// <summary>
/// The outcome of a tool call. Errors here are ones the model can read and act on (a closed event, a
/// missing argument); they are not protocol errors.
/// </summary>
public sealed record McpToolResult(JsonNode? StructuredContent, string Text, bool IsError)
{
    // Clients that predate structured content read the same JSON from the text block.
    public static McpToolResult Success(object payload)
    {
        var node = JsonSerializer.SerializeToNode(payload, McpJson.Options)!;
        return new McpToolResult(node, node.ToJsonString(McpJson.Options), false);
    }

    public static McpToolResult Error(string message) => new(null, message, true);
}

public static class JsonRpcErrorCodes
{
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;
    public const int HeaderMismatch = -32020;
    public const int UnsupportedProtocolVersion = -32022;
}

public static class McpJson
{
    // Relaxed escaping keeps apostrophes and accents readable in the text the model sees. The responses are
    // application/json, never embedded in HTML, so the HTML-safe escaping is not needed.
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
