using System.Globalization;
using System.Text.Json.Nodes;
using DjPortalApi.Features.Events;
using DjPortalApi.Features.Requests;
using DjPortalApi.Features.Tracks;
using Microsoft.Extensions.Logging;

namespace DjPortalApi.Features.Mcp;

/// <summary>
/// The MCP tools: the same four a dancer's browser gets through WebMCP (ui/src/webmcp.js), backed by the
/// services the REST API uses. Callers are always treated as visitors, never as the DJ.
/// </summary>
public sealed class McpTools(
    IEventService eventService,
    ITrackRepository trackRepository,
    IRequestService requestService,
    ILogger<McpTools> logger) : IMcpTools
{
    private const int MaxTrackLength = 200;
    private const int MaxNameLength = 50;

    private static readonly HashSet<string> ToolNames = ["list_upcoming_events", "search_tracks", "list_event_requests", "request_track"];

    public JsonArray ListTools() => JsonNode.Parse(ToolsJson)!.AsArray();

    public bool HasTool(string name) => ToolNames.Contains(name);

    public async Task<McpToolResult> CallAsync(string name, JsonObject arguments)
    {
        try
        {
            return name switch
            {
                "list_upcoming_events" => await ListUpcomingEventsAsync(),
                "search_tracks" => await SearchTracksAsync(arguments),
                "list_event_requests" => await ListEventRequestsAsync(arguments),
                "request_track" => await RequestTrackAsync(arguments),
                _ => McpToolResult.Error($"Unknown tool: {name}")
            };
        }
        catch (Exception ex)
        {
            // The caller is an anonymous agent, so it gets a plain message and the detail goes to the logs.
            logger.LogError(ex, "MCP tool {ToolName} failed.", name);
            return McpToolResult.Error("Something went wrong on DJ Mark's side. Try again in a moment.");
        }
    }

    private async Task<McpToolResult> ListUpcomingEventsAsync()
    {
        var events = await eventService.List();
        var upcoming = events
            .Where(x => !x.IsCancelled)
            .OrderBy(x => x.Date)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,
                description = x.Description,
                date = x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                times = x.Times,
                venue = x.LocationName,
                address = x.LocationAddress,
                djs = x.Djs,
                isRequestable = x.IsRequestable,
                eventPageUrl = x.EventPageUrl
            });

        return McpToolResult.Success(new { events = upcoming });
    }

    private async Task<McpToolResult> SearchTracksAsync(JsonObject arguments)
    {
        var query = GetString(arguments, "query")?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return McpToolResult.Error("query must not be empty.");
        }

        var lowBpm = GetDecimal(arguments, "lowBpm") ?? 100m;
        var highBpm = GetDecimal(arguments, "highBpm") ?? 150m;
        var tracks = await trackRepository.ListAsync(query, lowBpm, highBpm);

        return McpToolResult.Success(new
        {
            tracks = tracks.Select(t => new { track = t.Summary, title = t.Title, artist = t.Artist, time = t.Time, bpm = t.BPM })
        });
    }

    private async Task<McpToolResult> ListEventRequestsAsync(JsonObject arguments)
    {
        if (!TryGetEventId(arguments, out var eventId, out var error) || !TryGetRequestorId(arguments, out var requestorId, out error))
        {
            return error!;
        }

        // Without a requestor id nobody's requests are labelled "You", so use one that matches no one.
        var requests = await requestService.ListAsync(eventId, requestorId ?? Guid.NewGuid(), isAuthenticated: false);

        return McpToolResult.Success(new
        {
            requests = requests.Select(r => new { requestedBy = r.UserName, track = r.TrackName, status = r.Status })
        });
    }

    private async Task<McpToolResult> RequestTrackAsync(JsonObject arguments)
    {
        if (!TryGetEventId(arguments, out var eventId, out var error) || !TryGetRequestorId(arguments, out var requestorId, out error))
        {
            return error!;
        }

        var track = GetString(arguments, "track")?.Trim();
        if (string.IsNullOrEmpty(track) || track.Length > MaxTrackLength)
        {
            return McpToolResult.Error($"track must be between 1 and {MaxTrackLength} characters.");
        }

        var requestedBy = GetString(arguments, "requestedBy")?.Trim();
        if (string.IsNullOrEmpty(requestedBy) || requestedBy.Length > MaxNameLength)
        {
            return McpToolResult.Error($"requestedBy must be between 1 and {MaxNameLength} characters.");
        }

        // MCP clients don't keep cookies, so the requestor id stands in for the cookie the website uses. It is
        // returned so the agent can send it back for the same dancer and keep the 5-per-event limit working.
        var userId = requestorId ?? Guid.NewGuid();
        var model = new MusicRequestModel
        {
            EventId = eventId.ToString(),
            MusicRequest = track,
            RequestedBy = requestedBy,
            Bpm = GetDecimal(arguments, "bpm"),
            Time = GetString(arguments, "time")?.Trim()
        };

        var result = await requestService.CreateAsync(eventId, userId, isAuthenticated: false, model);
        if (result.Outcome != CreateRequestOutcome.Created)
        {
            return McpToolResult.Error(result.Message ?? "The request was not accepted.");
        }

        return McpToolResult.Success(new
        {
            status = result.Request?.Status,
            track = result.Request?.TrackName,
            requestorId = userId,
            message = $"Requested \"{track}\". DJ Mark will review it and choose what to play. Pass this requestorId on {requestedBy}'s later requests."
        });
    }

    private static bool TryGetEventId(JsonObject arguments, out Guid eventId, out McpToolResult? error)
    {
        error = Guid.TryParse(GetString(arguments, "eventId"), out eventId) && eventId != Guid.Empty
            ? null
            : McpToolResult.Error("eventId must be the id of an event from list_upcoming_events.");

        return error is null;
    }

    private static bool TryGetRequestorId(JsonObject arguments, out Guid? requestorId, out McpToolResult? error)
    {
        requestorId = null;
        error = null;

        var value = GetString(arguments, "requestorId");
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            requestorId = parsed;
            return true;
        }

        error = McpToolResult.Error("requestorId must be the value an earlier request_track call returned. Leave it out for a dancer's first request.");
        return false;
    }

    private static string? GetString(JsonObject arguments, string name)
        => arguments[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static decimal? GetDecimal(JsonObject arguments, string name)
    {
        if (arguments[name] is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<decimal>(out var number))
        {
            return number;
        }

        return value.TryGetValue<string>(out var text) && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }

    // Kept in step with the WebMCP tools in ui/src/webmcp.js, apart from requestorId: browsers have the cookie.
    private const string ToolsJson = """
        [
          {
            "name": "list_upcoming_events",
            "title": "List upcoming events",
            "description": "List the upcoming Ceroc dance events where DJ Mark is playing, soonest first. Use an event's id with request_track and list_event_requests. Only events with isRequestable true are taking song requests.",
            "inputSchema": { "type": "object", "additionalProperties": false },
            "annotations": { "readOnlyHint": true, "openWorldHint": false }
          },
          {
            "name": "search_tracks",
            "title": "Search DJ Mark's music library",
            "description": "Search DJ Mark's music library by title, artist or album. Returns up to 10 tracks. Tracks that are not in the library can still be requested, but a match gives DJ Mark the exact track.",
            "inputSchema": {
              "type": "object",
              "properties": {
                "query": { "type": "string", "minLength": 1, "description": "Words from the track title, artist or album." },
                "lowBpm": { "type": "number", "default": 100, "description": "Lowest BPM to include." },
                "highBpm": { "type": "number", "default": 150, "description": "Highest BPM to include." }
              },
              "required": ["query"],
              "additionalProperties": false
            },
            "annotations": { "readOnlyHint": true, "openWorldHint": false }
          },
          {
            "name": "list_event_requests",
            "title": "List requests for an event",
            "description": "List the song requests made so far for an event and their status (Pending, Approved, Queued, Played or AlreadyServed). Other dancers appear under pseudonyms and their unapproved tracks read \"Pending Approval\". Pass a dancer's requestorId to see their own requests labelled \"You\".",
            "inputSchema": {
              "type": "object",
              "properties": {
                "eventId": { "type": "string", "format": "uuid", "description": "The event id from list_upcoming_events." },
                "requestorId": { "type": "string", "format": "uuid", "description": "The requestorId that request_track returned for this dancer, if any." }
              },
              "required": ["eventId"],
              "additionalProperties": false
            },
            "annotations": { "readOnlyHint": true, "openWorldHint": false }
          },
          {
            "name": "request_track",
            "title": "Request a track",
            "description": "Send DJ Mark a song request for an event on a dancer's behalf. Only use this when the dancer has asked for the song, and use the name they give. DJ Mark reviews every request and chooses what to play. Each dancer may make up to 5 requests per event. If search_tracks found the track, pass its track, bpm and time. The result includes a requestorId: pass it on later requests for the same dancer so their requests stay together and count towards their limit.",
            "inputSchema": {
              "type": "object",
              "properties": {
                "eventId": { "type": "string", "format": "uuid", "description": "The event id from list_upcoming_events." },
                "track": { "type": "string", "minLength": 1, "maxLength": 200, "description": "The song as \"Title, Artist\"." },
                "requestedBy": { "type": "string", "minLength": 1, "maxLength": 50, "description": "The dancer's first name, shown to DJ Mark." },
                "bpm": { "type": "number", "description": "The track's BPM from search_tracks, if known." },
                "time": { "type": "string", "description": "The track's length from search_tracks, if known." },
                "requestorId": { "type": "string", "format": "uuid", "description": "The requestorId an earlier request_track call returned for this dancer. Leave it out for their first request." }
              },
              "required": ["eventId", "track", "requestedBy"],
              "additionalProperties": false
            },
            "annotations": { "readOnlyHint": false, "destructiveHint": false, "idempotentHint": false, "openWorldHint": false }
          }
        ]
        """;
}
