using System.ComponentModel.DataAnnotations;

namespace DjPortalApi.Features.Import.Models;

public sealed class ImportEventModel
{
    [Required]
    public string? Url { get; set; }
}
