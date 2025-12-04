using System.ComponentModel.DataAnnotations;

namespace DjPortalApi.Features.Events;

public class CreateEventModel : IEventDetailsData
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Description { get; set; }

    [Required]
    public string? LocationName { get; set; }

    [Required]
    public string? LocationAddress { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [RegularExpression(@"[0-2][0-9]:[0-5][0-9] - [0-2][0-9]:[0-5][0-9]")]
    public string? Times { get; set; }

    public string? FacebookEventId { get; set; }

    public string? Tags { get; set; }

    public bool IsRequestable { get; set; }

    public bool GenerateSchemaData { get; set; }

    public bool IsCancelled { get; set; }
}
