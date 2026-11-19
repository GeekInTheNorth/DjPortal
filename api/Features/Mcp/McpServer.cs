using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static DjPortalApi.Features.Mcp.JsonRpcErrorCodes;

namespace DjPortalApi.Features.Mcp;

/// <summary>
/// A stateless MCP server for the Streamable HTTP transport. It offers tools only and answers every request
/// with a single JSON response, never an event stream. It serves two eras of the protocol on one endpoint:
/// <list type="bullet">
/// <item>2026-07-28 (modern): no handshake. Each request carries its protocol version and client capabilities
/// in params._meta, and mirrors the version, method and tool name into headers that must match the body.</item>
/// <item>2025-03-26 to 2025-11-25 (legacy): the client opens with initialize. No session id is issued, so
/// every later request can still be answered on its own.</item>
/// </list>
/// </summary>
public sealed class McpServer(IMcpTools tools) : IMcpServer
{
    private const string MetaKey = "_meta";
    private const string ProtocolVersionKey = "io.modelcontextprotocol/protocolVersion";
    private const string ClientCapabilitiesKey = "io.modelcontextprotocol/clientCapabilities";
    private const string ServerInfoKey = "io.modelcontextprotocol/serverInfo";

    // Clients that send no MCP-Protocol-Version header predate it, and the spec says to assume this version.
    private const string DefaultLegacyVersion = "2025-03-26";

    // The tools and server details only change on deploy, so clients may cache them for an hour.
    private const int CacheTtlMs = 3_600_000;

    public async Task<McpHttpResult> HandleAsync(string body, McpRequestHeaders headers)
    {
        JsonNode? message;
        try
        {
            message = JsonNode.Parse(body);
            ThrowOnDuplicateKeys(message);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return McpHttpResult.Error(HttpStatusCode.BadRequest, null, ParseError, "Parse error: the body is not valid JSON.");
        }

        if (message is not JsonObject request)
        {
            // Batches were dropped from the protocol in 2025-06-18.
            var reason = message is JsonArray ? "Batches are not supported. Send one message per request." : "The body must be a JSON-RPC message.";
            return McpHttpResult.Error(HttpStatusCode.BadRequest, null, InvalidRequest, reason);
        }

        var id = request["id"];
        var readableId = IsValidId(id) ? id : null;
        if (GetString(request, "jsonrpc") != "2.0")
        {
            return McpHttpResult.Error(HttpStatusCode.BadRequest, readableId, InvalidRequest, "jsonrpc must be \"2.0\".");
        }

        var method = GetString(request, "method");
        if (method is null)
        {
            // A response to a server-initiated request. This server never makes any, so there is nothing to do.
            return request.ContainsKey("id") && (request.ContainsKey("result") || request.ContainsKey("error"))
                ? McpHttpResult.Accepted
                : McpHttpResult.Error(HttpStatusCode.BadRequest, readableId, InvalidRequest, "method is required.");
        }

        // A notification, such as notifications/initialized. None needs any work, and none gets a reply.
        if (!request.ContainsKey("id"))
        {
            return McpHttpResult.Accepted;
        }

        if (readableId is null)
        {
            return McpHttpResult.Error(HttpStatusCode.BadRequest, null, InvalidRequest, "id must be a string or an integer.");
        }

        var parameters = request["params"] as JsonObject;
        if (method == "initialize")
        {
            return Initialize(readableId, parameters);
        }

        var metaVersion = GetString(parameters?[MetaKey] as JsonObject, ProtocolVersionKey);
        if (headers.ProtocolVersion is not null && metaVersion is not null && headers.ProtocolVersion != metaVersion)
        {
            return McpHttpResult.Error(HttpStatusCode.BadRequest, readableId, HeaderMismatch,
                $"Header mismatch: MCP-Protocol-Version header value '{headers.ProtocolVersion}' does not match body value '{metaVersion}'.");
        }

        var version = headers.ProtocolVersion ?? metaVersion ?? DefaultLegacyVersion;
        if (!McpServerInfo.SupportedVersions.Contains(version))
        {
            return McpHttpResult.Error(HttpStatusCode.BadRequest, readableId, UnsupportedProtocolVersion, "Unsupported protocol version",
                new JsonObject { ["supported"] = SupportedVersions(), ["requested"] = version });
        }

        return version == McpServerInfo.ModernVersion
            ? await HandleModernAsync(readableId, method, parameters, headers)
            : await HandleLegacyAsync(readableId, method, parameters);
    }

    private async Task<McpHttpResult> HandleModernAsync(JsonNode id, string method, JsonObject? parameters, McpRequestHeaders headers)
    {
        var meta = parameters?[MetaKey] as JsonObject;
        if (GetString(meta, ProtocolVersionKey) is null || meta?[ClientCapabilitiesKey] is not JsonObject)
        {
            return McpHttpResult.Error(HttpStatusCode.BadRequest, id, InvalidParams,
                $"params._meta must include {ProtocolVersionKey} and {ClientCapabilitiesKey}.");
        }

        var headerError = ValidateHeaders(id, method, parameters, headers);
        if (headerError is not null)
        {
            return headerError;
        }

        switch (method)
        {
            case "server/discover":
                return Result(id, modern: true, new JsonObject
                {
                    ["supportedVersions"] = SupportedVersions(),
                    ["capabilities"] = Capabilities(),
                    ["instructions"] = McpServerInfo.Instructions,
                    ["ttlMs"] = CacheTtlMs,
                    ["cacheScope"] = "public"
                });

            case "tools/list":
                return Result(id, modern: true, new JsonObject
                {
                    ["tools"] = tools.ListTools(),
                    ["ttlMs"] = CacheTtlMs,
                    ["cacheScope"] = "public"
                });

            case "tools/call":
                return await CallToolAsync(id, parameters, modern: true);

            default:
                // A 404 with this error lets clients tell an unknown method from a missing endpoint.
                return McpHttpResult.Error(HttpStatusCode.NotFound, id, MethodNotFound, $"Method not found: {method}");
        }
    }

    private async Task<McpHttpResult> HandleLegacyAsync(JsonNode id, string method, JsonObject? parameters)
    {
        return method switch
        {
            "ping" => Result(id, modern: false, new JsonObject()),
            "tools/list" => Result(id, modern: false, new JsonObject { ["tools"] = tools.ListTools() }),
            "tools/call" => await CallToolAsync(id, parameters, modern: false),
            // Legacy clients read JSON-RPC errors from a 200; a 4xx would look to them like a broken transport.
            _ => McpHttpResult.Error(HttpStatusCode.OK, id, MethodNotFound, $"Method not found: {method}")
        };
    }

    /// <summary>
    /// The legacy handshake. The client's version is echoed back if this server speaks it; otherwise the
    /// newest handshake version is offered and the client decides whether to carry on.
    /// </summary>
    private static McpHttpResult Initialize(JsonNode id, JsonObject? parameters)
    {
        var requested = GetString(parameters, "protocolVersion");
        var negotiated = requested is not null && McpServerInfo.LegacyVersions.Contains(requested)
            ? requested
            : McpServerInfo.LegacyVersions[0];

        return Result(id, modern: false, new JsonObject
        {
            ["protocolVersion"] = negotiated,
            ["capabilities"] = Capabilities(),
            ["serverInfo"] = McpServerInfo.Implementation(),
            ["instructions"] = McpServerInfo.Instructions
        });
    }

    private async Task<McpHttpResult> CallToolAsync(JsonNode id, JsonObject? parameters, bool modern)
    {
        // Failing to find the tool is a protocol error. Bad arguments are a tool error the model can see and fix.
        var name = GetString(parameters, "name");
        if (name is null)
        {
            return McpHttpResult.Error(HttpStatusCode.OK, id, InvalidParams, "params.name is required.");
        }

        if (!tools.HasTool(name))
        {
            return McpHttpResult.Error(HttpStatusCode.OK, id, InvalidParams, $"Unknown tool: {name}");
        }

        var arguments = parameters?["arguments"];
        var outcome = arguments is null or JsonObject
            ? await tools.CallAsync(name, arguments as JsonObject ?? new JsonObject())
            : McpToolResult.Error("arguments must be an object.");

        var result = new JsonObject
        {
            ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = outcome.Text })
        };

        if (outcome.StructuredContent is not null)
        {
            result["structuredContent"] = outcome.StructuredContent;
        }

        if (outcome.IsError)
        {
            result["isError"] = true;
        }

        return Result(id, modern, result);
    }

    /// <summary>
    /// Gateways may route on the mirrored headers, so they must say the same as the body. Applies to the
    /// modern era only; legacy clients don't send these headers.
    /// </summary>
    private static McpHttpResult? ValidateHeaders(JsonNode id, string method, JsonObject? parameters, McpRequestHeaders headers)
    {
        string? problem = null;
        if (headers.ProtocolVersion is null)
        {
            problem = "The MCP-Protocol-Version header is required.";
        }
        else if (headers.Method is null)
        {
            problem = "The Mcp-Method header is required.";
        }
        else if (headers.Method != method)
        {
            problem = $"Header mismatch: Mcp-Method header value '{headers.Method}' does not match body value '{method}'.";
        }
        else if (method == "tools/call")
        {
            var bodyName = GetString(parameters, "name");
            var headerName = headers.Name is null ? null : DecodeHeaderValue(headers.Name);
            if (headers.Name is null)
            {
                problem = "The Mcp-Name header is required for tools/call.";
            }
            else if (headerName != bodyName)
            {
                problem = $"Header mismatch: Mcp-Name header value '{headerName}' does not match body value '{bodyName}'.";
            }
        }

        return problem is null ? null : McpHttpResult.Error(HttpStatusCode.BadRequest, id, HeaderMismatch, problem);
    }

    /// <summary>Header values that aren't plain ASCII arrive Base64-encoded as =?base64?...?=.</summary>
    private static string? DecodeHeaderValue(string value)
    {
        const string prefix = "=?base64?";
        const string suffix = "?=";
        if (value.Length < prefix.Length + suffix.Length || !value.StartsWith(prefix, StringComparison.Ordinal) || !value.EndsWith(suffix, StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value[prefix.Length..^suffix.Length]));
        }
        catch (FormatException)
        {
            // Null never matches a tool name, so a bad encoding is reported as a mismatch.
            return null;
        }
    }

    private static McpHttpResult Result(JsonNode id, bool modern, JsonObject result)
    {
        if (modern)
        {
            result["resultType"] = "complete";
            result[MetaKey] = new JsonObject { [ServerInfoKey] = McpServerInfo.Implementation() };
        }

        return new McpHttpResult(HttpStatusCode.OK, new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id.DeepClone(),
            ["result"] = result
        });
    }

    /// <summary>
    /// JsonNode only notices a repeated property name when an object is first read, which would otherwise
    /// surface later as a 500. Reading the whole tree here turns it into a parse error.
    /// </summary>
    private static void ThrowOnDuplicateKeys(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj)
                {
                    ThrowOnDuplicateKeys(property.Value);
                }

                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    ThrowOnDuplicateKeys(item);
                }

                break;
        }
    }

    private static JsonObject Capabilities() => new() { ["tools"] = new JsonObject() };

    private static JsonArray SupportedVersions() => new(McpServerInfo.SupportedVersions.Select(v => (JsonNode?)v).ToArray());

    // JSON-RPC ids must be a string or an integer; MCP also forbids null.
    private static bool IsValidId(JsonNode? id) => id is JsonValue value && value.GetValueKind() switch
    {
        JsonValueKind.String => true,
        JsonValueKind.Number => value.TryGetValue<long>(out _),
        _ => false
    };

    private static string? GetString(JsonObject? source, string name)
        => source?[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
