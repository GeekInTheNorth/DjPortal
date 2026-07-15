using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using DjPortalApi.Features.Events;
using DjPortalApi.Features.Requests;
using DjPortalApi.Features.Tracks;
using DjPortalApi.Features.WebSearch;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace DjPortalApi.Features.AiChat;

public sealed class AiChatService : IAiChatService
{
    private const int MaxToolIterations = 5;

    private const string DefaultRequestorName = "From the Floor";

    private readonly ChatClient? _chatClient;
    private readonly ITrackRepository _trackRepository;
    private readonly IWebSearchService _webSearchService;
    private readonly IRequestService _requestService;
    private readonly IRequestRepository _requestRepository;

    public AiChatService(
        IConfiguration configuration,
        ITrackRepository trackRepository,
        IWebSearchService webSearchService,
        IRequestService requestService,
        IRequestRepository requestRepository)
    {
        _trackRepository = trackRepository;
        _webSearchService = webSearchService;
        _requestService = requestService;
        _requestRepository = requestRepository;

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

    public async Task<AiChatResponse> SendAsync(EventDetails eventDetails, Guid userId, bool isAuthenticated, IList<AiChatMessageModel> history)
    {
        if (_chatClient is null)
        {
            return new AiChatResponse
            {
                Reply = "The AI assistant isn't available right now — please use the standard request form below.",
                RequestSubmitted = false
            };
        }

        var knownName = await _requestRepository.GetUserName(userId);

        var messages = new List<ChatMessage> { new SystemChatMessage(BuildSystemPrompt(eventDetails, knownName)) };
        foreach (var message in history)
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

        var options = new ChatCompletionOptions
        {
            Tools = { SearchTracksTool, WebSearchTool, SubmitRequestTool, PresentOptionsTool }
        };

        var requestSubmitted = false;
        List<string>? quickReplies = null;

        for (var iteration = 0; iteration < MaxToolIterations; iteration++)
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(completion));
                foreach (var toolCall in completion.ToolCalls)
                {
                    // present_options only carries UI quick-replies back to the caller; it does no work.
                    if (string.Equals(toolCall.FunctionName, "present_options", StringComparison.Ordinal))
                    {
                        quickReplies = ParseOptions(toolCall);
                        messages.Add(new ToolChatMessage(toolCall.Id, "{\"shown\":true}"));
                        continue;
                    }

                    var (resultJson, submitted) = await ExecuteToolAsync(toolCall, eventDetails, userId, isAuthenticated, knownName);
                    requestSubmitted = requestSubmitted || submitted;
                    messages.Add(new ToolChatMessage(toolCall.Id, resultJson));
                }

                continue;
            }

            var reply = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;
            return new AiChatResponse { Reply = reply, RequestSubmitted = requestSubmitted, Options = quickReplies };
        }

        return new AiChatResponse
        {
            Reply = "Sorry, I couldn't finish that. Please try again, or use the standard request form below.",
            RequestSubmitted = requestSubmitted,
            Options = quickReplies
        };
    }

    private async Task<(string resultJson, bool submitted)> ExecuteToolAsync(
        ChatToolCall toolCall,
        EventDetails eventDetails,
        Guid userId,
        bool isAuthenticated,
        string? knownName)
    {
        JsonElement root;
        try
        {
            root = JsonDocument.Parse(toolCall.FunctionArguments.ToString()).RootElement;
        }
        catch (JsonException)
        {
            return (JsonSerializer.Serialize(new { error = "Invalid arguments." }), false);
        }

        switch (toolCall.FunctionName)
        {
            case "search_tracks":
            {
                var query = GetString(root, "query");
                var lowBpm = GetDecimal(root, "lowBpm") ?? 100m;
                var highBpm = GetDecimal(root, "highBpm") ?? 145m;
                var tracks = await _trackRepository.ListAsync(query, lowBpm, highBpm);
                var results = tracks.Select(t => new { title = t.Title, artist = t.Artist, bpm = t.BPM, time = t.Time });
                return (JsonSerializer.Serialize(new { results }), false);
            }

            case "web_search":
            {
                var query = GetString(root, "query") ?? string.Empty;
                var hits = await _webSearchService.Search(query, 5);
                var results = hits.Select(r => new { title = r.Title, url = r.Url, snippet = r.Content });
                return (JsonSerializer.Serialize(new { results }), false);
            }

            case "submit_request":
            {
                var spotifyUrl = GetString(root, "spotifyUrl");
                var trackName = GetString(root, "trackName");
                var requestedBy = GetString(root, "requestedBy");

                if (string.IsNullOrWhiteSpace(trackName) && string.IsNullOrWhiteSpace(spotifyUrl))
                {
                    return (JsonSerializer.Serialize(new { success = false, error = "A track is required before submitting." }), false);
                }

                // Never block a submission on a missing name: fall back to the known cookie name, then a default.
                if (string.IsNullOrWhiteSpace(requestedBy))
                {
                    requestedBy = !string.IsNullOrWhiteSpace(knownName) ? knownName : DefaultRequestorName;
                }

                var model = new MusicRequestModel
                {
                    EventId = eventDetails.Id.ToString(),
                    // A Spotify URL takes precedence so the request pipeline can enrich the track name.
                    MusicRequest = !string.IsNullOrWhiteSpace(spotifyUrl) ? spotifyUrl : trackName,
                    RequestedBy = requestedBy,
                    Bpm = GetDecimal(root, "bpm"),
                    Time = GetString(root, "time")
                };

                var result = await _requestService.CreateAsync(eventDetails.Id, userId, isAuthenticated, model);
                if (result.Outcome == CreateRequestOutcome.QuotaExceeded)
                {
                    return (JsonSerializer.Serialize(new { success = false, error = result.Message }), false);
                }

                return (JsonSerializer.Serialize(new { success = true, track = result.Request?.TrackName }), true);
            }

            default:
                return (JsonSerializer.Serialize(new { error = "Unknown tool." }), false);
        }
    }

    private static string BuildSystemPrompt(EventDetails eventDetails, string? knownName)
    {
        var nameGuidance = string.IsNullOrWhiteSpace(knownName)
            ? $"""
              - You do not know the dancer's name, but NEVER interrupt the flow to ask for it. When confirming a
                track, include 'Add my name' as one of the tappable options. Only if they tap it should you ask
                them to type a name. If they confirm without giving one, submit using '{DefaultRequestorName}'.
              """
            : $"""
              - The dancer is known as '{knownName}' from their previous requests. Use this name silently and NEVER
                ask for it — only change it if they explicitly give a different name.
              """;

        return $"""
            You are DJ Mark's friendly music request assistant for the event "{eventDetails.Name}".
            This is a modern jive / ceroc dance event, so tracks should be danceable at a partner-dance tempo.
            Help the dancer find a track and submit a request.

            Finding music:
            - When the dancer is vague (e.g. "some swing", "something upbeat", "a smoochy one"), use your own
              music knowledge to brainstorm several SPECIFIC artists and songs that fit the vibe AND suit modern
              jive dancing (think what a good ceroc DJ would play for that request).
            - Call search_tracks first to check DJ Mark's own library and prefer tracks it returns.
            - Use web_search when you need to confirm a track really exists, or to find current/recent releases and
              chart hits that may be beyond your own knowledge (e.g. an artist's latest single). Cross-reference what
              you find, then suggest real tracks.
            - Offer a short shortlist of concrete options by name rather than asking the dancer to be more specific.
            - Whenever your message asks the dancer to choose, call present_options so they can TAP their answer
              instead of typing: offer each suggested track as an option, and for confirmations offer choices like
              'Yes, request it' and 'Show me others'. The option labels must be exactly what they'd reply.

            The requester's name:
            {nameGuidance}

            Submitting:
            - When the dancer picks a track, briefly acknowledge THAT specific track by name and move forward.
              NEVER re-list the earlier shortlist or repeat your previous message — that looks like a failure.
            - Confirm with tappable options via present_options (e.g. 'Yes, request it', 'Add my name',
              'Show me others'), then call submit_request as soon as they confirm.
            - After submit_request succeeds, reply with a short confirmation like "Done — I've sent your request to
              DJ Mark!" and do NOT show any options.
            - When a submission fails, relay the returned error message to the user word for word.

            Keep replies short and warm. Suggest real songs and artists — never make up song titles that do not exist.
            """;
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
                    .Select(x => x.GetString()!)
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

    private static string? GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal? GetDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static readonly ChatTool SearchTracksTool = ChatTool.CreateFunctionTool(
        "search_tracks",
        "Search DJ Mark's own music library. Use this first. Returns up to 10 tracks, each with title, artist, bpm and time.",
        BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "query": { "type": "string", "description": "Artist, song name or keywords to search for." },
                "lowBpm": { "type": "number", "description": "Optional minimum beats per minute." },
                "highBpm": { "type": "number", "description": "Optional maximum beats per minute." }
              },
              "required": ["query"]
            }
            """));

    private static readonly ChatTool PresentOptionsTool = ChatTool.CreateFunctionTool(
        "present_options",
        "Show the dancer tappable quick-reply buttons so they don't have to type. Call this whenever your message asks them to choose — track shortlists, or confirmations. Provide 2 to 5 short options.",
        BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "options": {
                  "type": "array",
                  "items": { "type": "string" },
                  "description": "2-5 short button labels, each a natural reply the dancer might tap, e.g. 'Light Up - Kylie Minogue', 'Yes, request it' or 'Show me others'."
                }
              },
              "required": ["options"]
            }
            """));

    private static readonly ChatTool WebSearchTool = ChatTool.CreateFunctionTool(
        "web_search",
        "Search the web to confirm a real track exists or to find current/recent releases and chart hits. Returns title, url and snippet.",
        BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "query": { "type": "string", "description": "A concise search phrase, e.g. 'Kylie Minogue latest single' or 'best modern jive swing tracks'." }
              },
              "required": ["query"]
            }
            """));

    private static readonly ChatTool SubmitRequestTool = ChatTool.CreateFunctionTool(
        "submit_request",
        "Submit the chosen track as a request for this event. Only call after confirming the track with the user.",
        BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "trackName": { "type": "string", "description": "The track as 'Title, Artist'." },
                "requestedBy": { "type": "string", "description": "The dancer's name if known; omit if they haven't given one." },
                "bpm": { "type": "number", "description": "Optional beats per minute from a library result." },
                "time": { "type": "string", "description": "Optional timing/length from a library result." },
                "spotifyUrl": { "type": "string", "description": "A spotify track url ONLY if the dancer pasted one; otherwise omit." }
              },
              "required": ["trackName"]
            }
            """));
}
