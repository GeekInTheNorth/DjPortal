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

// What a tool call produced: the JSON handed back to the model, and whether it actually put a request
// in the database (which the caller needs so the UI can show the tick and refresh the list).
public sealed record AiChatToolResult(string ResultJson, bool RequestSubmitted);

public sealed class AiChatResponse
{
    private List<string>? _options;

    public string Reply { get; set; } = string.Empty;

    public bool RequestSubmitted { get; set; }

    // Set when Azure's content filter blocked this turn. Tells the client to stop resending the
    // message that tripped it, so the next turn isn't rejected for the same reason.
    public bool ContentFiltered { get; set; }

    public List<string>? Options
    {
        get => RequestSubmitted ? [] : _options;
        set => _options = value;
    }
}
