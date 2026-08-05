using System.ClientModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.OpenAI;
using DjPortalApi.Features.Events;
using DjPortalApi.Features.Requests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace DjPortalApi.Features.AiChat;

public sealed partial class AiChatService : IAiChatService
{
    private readonly ChatClient? _chatClient;

    private readonly IAiChatTools _tools;

    private readonly IRequestRepository _requestRepository;

    private readonly ILogger<AiChatService> _logger;
    
    private const int MaxToolIterations = 10;

    // Only the most recent chat entries are sent to the model to cap token growth.
    private const int MaxHistoryMessages = 20;

    // The chat bubbles render plain text, so any Markdown the model emits would show as raw punctuation.
    [GeneratedRegex(@"\[([^\]\n]+)\]\((https?://[^\s)]+)\)")]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"`+([^`\n]+)`+")]
    private static partial Regex MarkdownCode();

    [GeneratedRegex(@"(\*\*|__)(?=\S)(.+?)(?<=\S)\1", RegexOptions.Singleline)]
    private static partial Regex MarkdownBold();

    [GeneratedRegex(@"(?<![\w*_])([*_])(?=\S)([^*_\n]+?)(?<=\S)\1(?![\w*_])")]
    private static partial Regex MarkdownItalic();

    [GeneratedRegex(@"^[ \t]{0,3}#{1,6}[ \t]+", RegexOptions.Multiline)]
    private static partial Regex MarkdownHeading();

    public AiChatService(
        IConfiguration configuration,
        IAiChatTools tools,
        IRequestRepository requestRepository,
        ILogger<AiChatService> logger)
    {
        _tools = tools;
        _requestRepository = requestRepository;
        _logger = logger;

        var endpoint = configuration.GetValue<string>("AzureOpenAiEndpoint");
        var apiKey = configuration.GetValue<string>("AzureOpenAiApiKey");
        var deployment = configuration.GetValue<string>("AzureOpenAiDeployment");

        if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(deployment)
            && Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            // Accept either the base resource endpoint or a Foundry project endpoint
            // (…/api/projects/…) — AzureOpenAIClient needs just scheme + host.
            var baseUri = new Uri(endpointUri, "/");
            var client = new AzureOpenAIClient(baseUri, new AzureKeyCredential(apiKey));
            _chatClient = client.GetChatClient(deployment);
        }
    }

    public async Task<AiChatResponse> SendAsync(EventDetails eventDetails, Guid userId, bool isAuthenticated, IList<AiChatMessageModel> history, AiChatMode mode)
    {
        if (_chatClient is null)
        {
            return new AiChatResponse
            {
                Reply = "The AI assistant isn't available right now — please use the standard request form below.",
                RequestSubmitted = false
            };
        }

        // In DJ mode the cookie belongs to the DJ's browser, and his submissions are given a fresh
        // user id anyway, so any name held against it would be the wrong dancer.
        var knownName = mode == AiChatMode.Dj ? null : await _requestRepository.GetUserName(userId);

        var messages = new List<ChatMessage> { new SystemChatMessage(AiChatPrompts.Build(eventDetails, knownName, mode)) };
        foreach (var message in history.TakeLast(MaxHistoryMessages))
        {
            var content = message.Content ?? string.Empty;
            if (string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new AssistantChatMessage(content));
            }
            else
            {
                messages.Add(new UserChatMessage(content));
            }
        }

        var options = new ChatCompletionOptions();
        foreach (var tool in _tools.GetTools())
        {
            options.Tools.Add(tool);
        }

        var requestSubmitted = false;
        List<string>? quickReplies = null;

        for (var iteration = 0; iteration < MaxToolIterations; iteration++)
        {
            ChatCompletion completion;
            try
            {
                completion = await _chatClient.CompleteChatAsync(messages, options);
            }
            // Only a content-filter rejection is handled here; everything else (throttling, a bad key,
            // a timeout) still propagates untouched. Returning immediately is deliberate — retrying the
            // same blocked payload would just fail MaxToolIterations times over.
            catch (ClientResultException ex) when (IsContentFilter(ex, out var categories))
            {
                _logger.LogWarning(
                    "Azure OpenAI blocked the AI chat prompt. Mode: {Mode}, iteration: {Iteration}, categories: {Categories}, messages: {MessageCount}.",
                    mode, iteration, categories, messages.Count);

                return BuildContentFilteredResponse(mode, iteration, requestSubmitted);
            }

            // A 200 whose finish reason is ContentFilter means the model's own output was blocked.
            if (completion.FinishReason == ChatFinishReason.ContentFilter)
            {
                _logger.LogWarning(
                    "Azure OpenAI blocked the AI chat completion. Mode: {Mode}, iteration: {Iteration}.",
                    mode, iteration);

                return BuildContentFilteredResponse(mode, iteration, requestSubmitted);
            }

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(completion));
                foreach (var toolCall in completion.ToolCalls)
                {
                    // present_options only carries UI quick-replies back to the caller; it does no work.
                    if (string.Equals(toolCall.FunctionName, AiChatTools.PresentOptionsToolName, StringComparison.Ordinal))
                    {
                        quickReplies = ParseOptions(toolCall);
                        messages.Add(new ToolChatMessage(toolCall.Id, AiChatTools.PresentOptionsResult));
                        continue;
                    }

                    var result = await _tools.ExecuteToolAsync(toolCall, eventDetails, userId, isAuthenticated, knownName);
                    requestSubmitted = requestSubmitted || result.RequestSubmitted;
                    messages.Add(new ToolChatMessage(toolCall.Id, result.ResultJson));
                }

                continue;
            }

            var reply = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;
            return new AiChatResponse { Reply = StripMarkdown(reply), RequestSubmitted = requestSubmitted, Options = quickReplies };
        }

        return new AiChatResponse
        {
            Reply = requestSubmitted ? "Your request has been submitted to DJ Mark." : "Sorry, I couldn't finish that. Please try again, or use the standard request form below.",
            RequestSubmitted = requestSubmitted,
            Options = quickReplies
        };
    }

    // Iteration 0 means the payload was the system prompt plus the client's own history, so the message
    // the dancer just sent is the culprit. Later iterations also carry tool results they never see.
    private static AiChatResponse BuildContentFilteredResponse(AiChatMode mode, int iteration, bool requestSubmitted)
    {
        var reply = (mode, iteration) switch
        {
            (AiChatMode.Dj, 0) => "That tripped Azure's content filter, so nothing reached the model. I've dropped it from the conversation — rephrase it and we'll carry on.",
            (AiChatMode.Dj, _) => "A search result tripped Azure's content filter part way through, so I stopped there. Try narrowing it down, or add the track straight from the request form.",
            (_, 0) => "Sorry — I couldn't send that one through. It's not you: the AI is fussy about certain wording. Try the artist or the song title instead, or a different line of the lyrics.",
            _ => "Sorry — I turned up something I'm not able to repeat back. Let's try another angle: do you know the artist, or roughly when it came out?"
        };

        return new AiChatResponse
        {
            Reply = reply,
            // The filter can trip after a request was already submitted, so the tick and the list refresh
            // still need to happen.
            RequestSubmitted = requestSubmitted,
            ContentFiltered = true,
            // Any earlier quick-replies belong to a conversation that just hit a wall — offer a way out instead.
            Options = mode == AiChatMode.Dancer ? ["I know the artist", "I know the title", "Just suggest something"] : null
        };
    }

    // Azure rejects a filtered prompt with a 400 before the model sees it. Never throws: it is used as an
    // exception filter, so a parsing problem must not decide whether the exception is handled.
    private static bool IsContentFilter(ClientResultException ex, out string categories)
    {
        categories = "unspecified";

        if (ex.Status != 400)
        {
            return false;
        }

        try
        {
            string? body = null;
            try
            {
                body = ex.GetRawResponse()?.Content?.ToString();
            }
            catch (InvalidOperationException)
            {
                // The response wasn't buffered — the message carries the same JSON.
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                body = ex.Message;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            using var document = JsonDocument.Parse(body);

            var error = document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var wrapped)
                    ? wrapped
                    : document.RootElement;

            if (error.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var inner = error.TryGetProperty("innererror", out var innerElement) && innerElement.ValueKind == JsonValueKind.Object
                ? innerElement
                : default;

            var filtered = string.Equals(error.GetString("code"), "content_filter", StringComparison.OrdinalIgnoreCase)
                || string.Equals(inner.GetString("code"), "ResponsibleAIPolicyViolation", StringComparison.OrdinalIgnoreCase);

            if (!filtered)
            {
                return false;
            }

            var tripped = ReadTrippedCategories(inner);
            if (tripped.Count > 0)
            {
                categories = string.Join(", ", tripped);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Severity categories report 'filtered' with a 'severity'; detection ones (jailbreak, profanity) report
    // 'detected' instead, so the shapes are read defensively rather than bound to a model.
    private static List<string> ReadTrippedCategories(JsonElement inner)
    {
        var tripped = new List<string>();

        if (inner.ValueKind != JsonValueKind.Object
            || !inner.TryGetProperty("content_filter_result", out var results)
            || results.ValueKind != JsonValueKind.Object)
        {
            return tripped;
        }

        foreach (var category in results.EnumerateObject())
        {
            if (category.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var isFiltered = category.Value.TryGetProperty("filtered", out var f) && f.ValueKind == JsonValueKind.True;
            var isDetected = category.Value.TryGetProperty("detected", out var d) && d.ValueKind == JsonValueKind.True;
            if (!isFiltered && !isDetected)
            {
                continue;
            }

            var severity = category.Value.GetString("severity");
            tripped.Add(severity is null ? category.Name : $"{category.Name}:{severity}");
        }

        return tripped;
    }

    private static string StripMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        text = MarkdownLink().Replace(text, "$1 ($2)");
        text = MarkdownCode().Replace(text, "$1");
        text = MarkdownBold().Replace(text, "$2");
        text = MarkdownItalic().Replace(text, "$2");
        text = MarkdownHeading().Replace(text, string.Empty);

        return text.Trim();
    }

    private static List<string>? ParseOptions(ChatToolCall toolCall)
    {
        try
        {
            var root = JsonDocument.Parse(toolCall.FunctionArguments.ToString()).RootElement;
            if (root.TryGetProperty("options", out var array) && array.ValueKind == JsonValueKind.Array)
            {
                var list = array.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => StripMarkdown(x.GetString()!))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                return list.Count > 0 ? list : null;
            }
        }
        catch (JsonException)
        {
            // Ignore malformed arguments — just show no quick-replies.
        }

        return null;
    }
}
