namespace DjPortalApi.Features.Mcp;

public interface IMcpServer
{
    /// <summary>Handles one POSTed JSON-RPC message and says what to send back.</summary>
    Task<McpHttpResult> HandleAsync(string body, McpRequestHeaders headers);
}
