using System.ClientModel;
using System.Globalization;
using System.Text.Json;
using DjPortalApi.Features.AiChat;
using DjPortalApi.Features.Common;
using DjPortalApi.Features.Import.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace DjPortalApi.Features.Import;

public sealed class EventImportService : IEventImportService
{
    private readonly ICerocClient _cerocClient;

    private readonly ILogger<EventImportService> _logger;

    private readonly ChatClient? _chatClient;

    private static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("en-GB");

    // Strict mode guarantees all four fields arrive, so the only thing left to check is that they aren't blank.
    private static readonly ChatResponseFormat DraftFormat = ChatResponseFormat.CreateJsonSchemaFormat(
        "event_draft",
        BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "name": {
                  "type": "string",
                  "description": "The event name as published, with only spacing and capitalisation tidied."
                },
                "description": {
                  "type": "string",
                  "description": "A single exciting paragraph of about 50 to 90 words describing the event."
                },
                "locationName": {
                  "type": "string",
                  "description": "The venue name only, e.g. 'Collingham Memorial Hall'."
                },
                "locationAddress": {
                  "type": "string",
                  "description": "The rest of the venue address, comma-separated with the postcode last in upper case, e.g. 'Main Street, Wetherby, LS22 5AS'."
                }
              },
              "required": ["name", "description", "locationName", "locationAddress"],
              "additionalProperties": false
            }
            """),
        jsonSchemaIsStrict: true);

    private static readonly ChatResponseFormat DescriptionFormat = ChatResponseFormat.CreateJsonSchemaFormat(
        "event_description",
        BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "description": {
                  "type": "string",
                  "description": "A single exciting paragraph of about 50 to 90 words describing the event."
                }
              },
              "required": ["description"],
              "additionalProperties": false
            }
            """),
        jsonSchemaIsStrict: true);

    public EventImportService(ICerocClient cerocClient, IConfiguration configuration, ILogger<EventImportService> logger)
    {
        _cerocClient = cerocClient;
        _logger = logger;
        _chatClient = AzureOpenAiChatClientFactory.Create(configuration);
    }

    public Task<IList<CerocEventListing>> GetListingsAsync(CancellationToken cancellationToken = default)
        => _cerocClient.GetFreestyleEventsAsync(cancellationToken);

    public async Task<EventDraft?> CreateDraftAsync(Uri eventUrl, CancellationToken cancellationToken = default)
    {
        if (_chatClient is null)
        {
            _logger.LogWarning("Event import skipped because Azure OpenAI is not configured.");
            return null;
        }

        var page = await _cerocClient.GetEventPageAsync(eventUrl, cancellationToken);
        if (string.IsNullOrWhiteSpace(page.Text))
        {
            _logger.LogWarning("The Ceroc event page {EventUrl} had no readable text.", eventUrl);
            return null;
        }

        var json = await CompleteJsonAsync(
            EventImportPrompts.ImportSystemPrompt,
            EventImportPrompts.BuildImportUserPrompt(eventUrl, page.Text),
            DraftFormat,
            $"event import for {eventUrl}",
            cancellationToken);

        if (!TryParseDraft(json, out var draft))
        {
            return null;
        }

        // The date and times come from the page's own "Event start" and "Event end" fields rather than the AI,
        // so they can't be misread or invented.
        draft.Date = page.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        draft.Times = page.StartTime is { } start && page.EndTime is { } end
            ? $"{start.ToString("HH:mm", CultureInfo.InvariantCulture)} - {end.ToString("HH:mm", CultureInfo.InvariantCulture)}"
            : null;
        draft.SourceUrl = eventUrl.ToString();

        return draft;
    }

    public async Task<string?> WriteDescriptionAsync(DescribeEventModel details, CancellationToken cancellationToken = default)
    {
        if (_chatClient is null)
        {
            _logger.LogWarning("Event description skipped because Azure OpenAI is not configured.");
            return null;
        }

        // The weekday is worked out here rather than left to the model, which can get it wrong.
        var formattedDate = DateOnly.TryParseExact(details.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString("dddd d MMMM yyyy", UkCulture)
            : null;

        var json = await CompleteJsonAsync(
            EventImportPrompts.DescribeSystemPrompt,
            EventImportPrompts.BuildDescribeUserPrompt(details, formattedDate),
            DescriptionFormat,
            "event description",
            cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var description = document.RootElement.GetString("description")?.Trim();
            return string.IsNullOrWhiteSpace(description) ? null : description;
        }
        catch (JsonException)
        {
            _logger.LogWarning("The event description reply was not valid JSON.");
            return null;
        }
    }

    // Sends one system and one user message held to the given schema. Returns the JSON text, or null (with the
    // reason logged) when the request is rejected, filtered or comes back empty. The DJ can always write the
    // details by hand, so none of these is worth more than a warning.
    private async Task<string?> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        ChatResponseFormat format,
        string operation,
        CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };
        var options = new ChatCompletionOptions { ResponseFormat = format };

        ChatCompletion completion;
        try
        {
            completion = await _chatClient!.CompleteChatAsync(messages, options, cancellationToken);
        }
        // Covers a content-filter rejection of the prompt as well as throttling or a bad key.
        catch (ClientResultException ex)
        {
            _logger.LogWarning(ex, "Azure OpenAI rejected the {Operation}. Status: {Status}.", operation, ex.Status);
            return null;
        }

        if (completion.FinishReason == ChatFinishReason.ContentFilter)
        {
            _logger.LogWarning("Azure OpenAI blocked the {Operation} completion.", operation);
            return null;
        }

        var json = completion.Content.Count > 0 ? completion.Content[0].Text : null;
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogWarning(
                "The {Operation} reply was empty. Finish reason: {FinishReason}, refused: {Refused}.",
                operation, completion.FinishReason, !string.IsNullOrWhiteSpace(completion.Refusal));
            return null;
        }

        return json;
    }

    private bool TryParseDraft(string? json, out EventDraft draft)
    {
        draft = new EventDraft();

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var name = root.GetString("name")?.Trim();
            var description = root.GetString("description")?.Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
            {
                _logger.LogWarning("The event import reply was missing a name or description.");
                return false;
            }

            draft.Name = name;
            draft.Description = description;
            draft.LocationName = root.GetString("locationName")?.Trim() ?? string.Empty;
            draft.LocationAddress = root.GetString("locationAddress")?.Trim() ?? string.Empty;

            return true;
        }
        catch (JsonException)
        {
            _logger.LogWarning("The event import reply was not valid JSON.");
            return false;
        }
    }
}
