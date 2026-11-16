namespace DjPortalApi.Features.Import.Models;

/// <summary>
/// The text and key times read from a Ceroc event page.
/// </summary>
public sealed class CerocEventPage
{
    public string Text { get; set; } = string.Empty;

    public DateOnly? StartDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }
}
