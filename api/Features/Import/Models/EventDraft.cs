namespace DjPortalApi.Features.Import.Models;

/// <summary>
/// A pre-filled event for the Create Event modal. Every field is a suggestion the DJ can change.
/// </summary>
public sealed class EventDraft
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string LocationAddress { get; set; } = string.Empty;

    /// <summary>
    /// The event date in yyyy-MM-dd format, or null when the page didn't give a start date.
    /// </summary>
    public string? Date { get; set; }

    /// <summary>
    /// The times in "HH:mm - HH:mm" format, or null when the page didn't give both a start and an end.
    /// </summary>
    public string? Times { get; set; }

    public string Organizer { get; set; } = "Ceroc Yorkshire";

    public string SourceUrl { get; set; } = string.Empty;
}
