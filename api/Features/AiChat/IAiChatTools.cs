using DjPortalApi.Features.Events;
using OpenAI.Chat;

namespace DjPortalApi.Features.AiChat;

public interface IAiChatTools
{
    IReadOnlyList<ChatTool> GetTools();

    Task<AiChatToolResult> ExecuteToolAsync(ChatToolCall toolCall, EventDetails eventDetails, Guid userId, bool isAuthenticated, string? knownName);
}
