using System.Text.Json.Nodes;

namespace DjPortalApi.Features.Mcp;

public interface IMcpTools
{
    /// <summary>The tool definitions for tools/list, as a fresh array the caller can attach to a response.</summary>
    JsonArray ListTools();

    bool HasTool(string name);

    Task<McpToolResult> CallAsync(string name, JsonObject arguments);
}
