using System.Text.Json;
using DjPortalApi.Features.Events;
using DjPortalApi.Features.Requests;
using DjPortalApi.Features.Tracks;
using DjPortalApi.Features.WebSearch;
using OpenAI.Chat;

namespace DjPortalApi.Features.AiChat;

// The tools the assistant can call, and the work each one does. Kept apart from AiChatService so that
// class is only the conversation loop.
public sealed class AiChatTools(
    ITrackRepository trackRepository,
    IWebSearchService webSearchService,
    IRequestService requestService) : IAiChatTools
{
    // present_options does no work — AiChatService intercepts it to pull out the quick-replies — but the
    // model still needs to see it in the tool list.
    public const string PresentOptionsToolName = "present_options";

    public const string PresentOptionsResult =
        "{\"shown\":true,\"note\":\"The options are now visible to the dancer as tappable buttons. Your reply text must NOT list, number or repeat them.\"}";

    // Web search snippets are enough to identify a track at this length; anything longer is mostly
    // verbatim page content going straight back through the content filter.
    private const int MaxSnippetLength = 300;

    public IReadOnlyList<ChatTool> GetTools() => [SearchTracksTool, WebSearchTool, SubmitRequestTool, PresentOptionsTool];

    public async Task<AiChatToolResult> ExecuteToolAsync(
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
            return Result(new { error = "Invalid arguments." });
        }

        switch (toolCall.FunctionName)
        {
            case "search_tracks":
            {
                var query = root.GetString("query");
                var lowBpm = root.GetDecimal("lowBpm") ?? 100m;
                var highBpm = root.GetDecimal("highBpm") ?? 145m;
                var tracks = await trackRepository.ListAsync(query, lowBpm, highBpm);
                var results = tracks.Select(t => new { title = t.Title, artist = t.Artist, bpm = t.BPM, time = t.Time });
                return Result(new { results });
            }

            case "web_search":
            {
                var query = root.GetString("query") ?? string.Empty;
                var hits = await webSearchService.Search(query, 5);
                // Snippets are third-party text and a lyric search returns lyrics pages verbatim — the most
                // likely thing in the payload to trip the content filter on the next call. Capping them also
                // keeps the token count down.
                var results = hits.Select(r => new { title = r.Title, url = r.Url, snippet = Truncate(r.Content, MaxSnippetLength) });
                return Result(new { results });
            }

            case "submit_request":
            {
                var link = root.GetString("link");
                var trackName = root.GetString("trackName");
                var requestedBy = root.GetString("requestedBy");

                if (string.IsNullOrWhiteSpace(trackName) && string.IsNullOrWhiteSpace(link))
                {
                    return Result(new { success = false, error = "A track is required before submitting." });
                }

                // Never block a submission on a missing name: fall back to the known cookie name, then a default.
                if (string.IsNullOrWhiteSpace(requestedBy))
                {
                    requestedBy = !string.IsNullOrWhiteSpace(knownName) ? knownName : AiChatPrompts.DefaultRequestorName;
                }

                var model = new MusicRequestModel
                {
                    EventId = eventDetails.Id.ToString(),
                    // A pasted link takes precedence so the request pipeline records it as a link.
                    MusicRequest = !string.IsNullOrWhiteSpace(link) ? link : trackName,
                    RequestedBy = requestedBy,
                    Bpm = root.GetDecimal("bpm"),
                    Time = root.GetString("time")
                };

                var result = await requestService.CreateAsync(eventDetails.Id, userId, isAuthenticated, model);
                if (result.Outcome == CreateRequestOutcome.QuotaExceeded)
                {
                    return Result(new { success = false, error = result.Message });
                }

                return Result(new { success = true, track = result.Request?.TrackName }, requestSubmitted: true);
            }

            default:
                return Result(new { error = "Unknown tool." });
        }
    }

    private static AiChatToolResult Result(object payload, bool requestSubmitted = false)
        => new(JsonSerializer.Serialize(payload), requestSubmitted);

    private static string? Truncate(string? value, int maxLength)
        => string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength] + "…";

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
        PresentOptionsToolName,
        "Show the dancer tappable quick-reply buttons so they don't have to type. Call this whenever your message asks them to choose — track shortlists, or confirmations. Provide 2 to 5 short options. The labels are rendered as buttons, so your accompanying message must not repeat them.",
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
                "link": { "type": "string", "description": "A track url ONLY if the dancer pasted one; otherwise omit." }
              },
              "required": ["trackName"]
            }
            """));
}
