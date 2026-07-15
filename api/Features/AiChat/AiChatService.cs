using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using DjPortalApi.Features.Events;
using DjPortalApi.Features.Requests;
using DjPortalApi.Features.Spotify;
using DjPortalApi.Features.Tracks;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace DjPortalApi.Features.AiChat;

public sealed class AiChatService : IAiChatService
{
    private const int MaxToolIterations = 5;

    private readonly ChatClient? _chatClient;
    private readonly ITrackRepository _trackRepository;
    private readonly ISpotifyService _spotifyService;
    private readonly IRequestService _requestService;

    public AiChatService(
        IConfiguration configuration,
        ITrackRepository trackRepository,
        ISpotifyService spotifyService,
        IRequestService requestService)
    {
        _trackRepository = trackRepository;
        _spotifyService = spotifyService;
        _requestService = requestService;

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

        var messages = new List<ChatMessage> { new SystemChatMessage(BuildSystemPrompt(eventDetails)) };
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
            Tools = { SearchTracksTool, SearchSpotifyTool, SubmitRequestTool }
        };

        var requestSubmitted = false;

        for (var iteration = 0; iteration < MaxToolIterations; iteration++)
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(completion));
                foreach (var toolCall in completion.ToolCalls)
                {
                    var (resultJson, submitted) = await ExecuteToolAsync(toolCall, eventDetails, userId, isAuthenticated);
                    requestSubmitted = requestSubmitted || submitted;
                    messages.Add(new ToolChatMessage(toolCall.Id, resultJson));
                }

                continue;
            }

            var reply = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;
            return new AiChatResponse { Reply = reply, RequestSubmitted = requestSubmitted };
        }

        return new AiChatResponse
        {
            Reply = "Sorry, I couldn't finish that. Please try again, or use the standard request form below.",
            RequestSubmitted = requestSubmitted
        };
    }

    private async Task<(string resultJson, bool submitted)> ExecuteToolAsync(
        ChatToolCall toolCall,
        EventDetails eventDetails,
        Guid userId,
        bool isAuthenticated)
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

            case "search_spotify":
            {
                var query = GetString(root, "query") ?? string.Empty;
                var tracks = await _spotifyService.SearchTracks(query);
                var results = tracks.Select(t => new
                {
                    title = t.Name,
                    artist = string.Join(", ", t.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string?>()),
                    url = t.ExternalUrl
                });
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

                if (string.IsNullOrWhiteSpace(requestedBy))
                {
                    return (JsonSerializer.Serialize(new { success = false, error = "The requester's name is required before submitting." }), false);
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

    private static string BuildSystemPrompt(EventDetails eventDetails)
    {
        return $"""
            You are DJ Mark's friendly music request assistant for the event "{eventDetails.Name}".
            Help the dancer find a track and submit a request.

            Rules:
            - Use search_tracks FIRST to look in DJ Mark's own library. Prefer suggesting tracks it returns.
            - Only use search_spotify if the library has nothing suitable. When submitting a Spotify result, pass its url as spotifyUrl.
            - Before calling submit_request you MUST know the exact track and the requester's name. Ask for the name if you don't have it. If the user declines to share their name, then use 'From the Floor' as their name.
            - Always confirm the track and name with the user in a message before calling submit_request.
            - When a submission fails, relay the returned error message to the user word for word.
            - Keep replies short and warm. Do not invent tracks that search tools did not return.
            """;
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

    private static readonly ChatTool SearchSpotifyTool = ChatTool.CreateFunctionTool(
        "search_spotify",
        "Search Spotify for tracks not found in DJ Mark's library. Returns title, artist and a spotify url. Use only as a fallback.",
        BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "query": { "type": "string", "description": "Artist, song name or keywords to search for." }
              },
              "required": ["query"]
            }
            """));

    private static readonly ChatTool SubmitRequestTool = ChatTool.CreateFunctionTool(
        "submit_request",
        "Submit the chosen track as a request for this event. Only call after confirming the track and requester name with the user.",
        BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "trackName": { "type": "string", "description": "The track as 'Title, Artist'." },
                "requestedBy": { "type": "string", "description": "The name the dancer gave." },
                "bpm": { "type": "number", "description": "Optional beats per minute from a library result." },
                "time": { "type": "string", "description": "Optional timing/length from a library result." },
                "spotifyUrl": { "type": "string", "description": "The spotify track url, when the track came from search_spotify." }
              },
              "required": ["trackName", "requestedBy"]
            }
            """));
}
