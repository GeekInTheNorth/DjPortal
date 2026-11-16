using System.ComponentModel.DataAnnotations;

namespace DjPortalApi.Features.Import;

/// <summary>
/// A single row from the Ceroc events listing.
/// </summary>
public sealed class CerocEventListing
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The town, as shown in the listing.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    public string Postcode { get; set; } = string.Empty;

    /// <summary>
    /// The event date in yyyy-MM-dd format.
    /// </summary>
    public string Date { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}

public sealed class ImportEventModel
{
    [Required]
    public string? Url { get; set; }
}

/// <summary>
/// The Create Event form as it currently stands, sent to have a new description written.
/// The length limits keep the prompt to a sensible size.
/// </summary>
public sealed class DescribeEventModel
{
    [Required]
    [StringLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// The current description, used as a source of facts for the new one.
    /// </summary>
    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? LocationName { get; set; }

    [StringLength(300)]
    public string? LocationAddress { get; set; }

    /// <summary>
    /// The event date in yyyy-MM-dd format.
    /// </summary>
    [StringLength(10)]
    public string? Date { get; set; }

    [StringLength(20)]
    public string? Times { get; set; }

    [StringLength(500)]
    public string? Tags { get; set; }

    [StringLength(100)]
    public string? Organizer { get; set; }

    public bool IsRequestable { get; set; }
}

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
