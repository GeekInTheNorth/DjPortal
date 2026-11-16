using System.ComponentModel.DataAnnotations;

namespace DjPortalApi.Features.Import.Models;

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
