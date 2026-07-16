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

public sealed class AiChatOption
{
    // What the dancer sees on the button.
    public string Label { get; set; } = string.Empty;

    // The message sent when the button is tapped (carries the intent, e.g. "Request Title - Artist").
    public string Value { get; set; } = string.Empty;
}

public sealed class AiChatResponse
{
    private List<AiChatOption>? _options;

    public string Reply { get; set; } = string.Empty;

    public bool RequestSubmitted { get; set; }

    public List<AiChatOption>? Options
    {
        get => RequestSubmitted ? [] : _options;
        set => _options = value;
    }
}
