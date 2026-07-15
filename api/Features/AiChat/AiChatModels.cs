namespace DjPortalApi.Features.AiChat;

public sealed class AiChatRequest
{
    public string? EventId { get; set; }

    public List<AiChatMessageModel>? Messages { get; set; }
}

public sealed class AiChatMessageModel
{
    public string? Role { get; set; }

    public string? Content { get; set; }
}

public sealed class AiChatResponse
{
    public string Reply { get; set; } = string.Empty;

    public bool RequestSubmitted { get; set; }
}
