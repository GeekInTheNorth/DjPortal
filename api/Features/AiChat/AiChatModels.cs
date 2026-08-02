namespace DjPortalApi.Features.AiChat;

public enum AiChatMode
{
    Dancer,
    Dj
}

public sealed class AiChatRequest
{
    public string? EventId { get; set; }

    // 'dj' switches the assistant to its DJ flavour, but only for an authenticated caller.
    public string? Mode { get; set; }

    public List<AiChatMessageModel>? Messages { get; set; }
}

public sealed class AiChatMessageModel
{
    public string? Role { get; set; }

    public string? Content { get; set; }
}

public sealed class AiChatResponse
{
    private List<string>? _options;

    public string Reply { get; set; } = string.Empty;

    public bool RequestSubmitted { get; set; }

    public List<string>? Options
    {
        get => RequestSubmitted ? [] : _options;
        set => _options = value;
    }
}
