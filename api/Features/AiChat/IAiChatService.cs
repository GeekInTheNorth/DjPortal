using DjPortalApi.Features.Events;

namespace DjPortalApi.Features.AiChat;

public interface IAiChatService
{
    Task<AiChatResponse> SendAsync(EventDetails eventDetails, Guid userId, bool isAuthenticated, IList<AiChatMessageModel> messages);
}
