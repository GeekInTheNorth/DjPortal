using System.ComponentModel.DataAnnotations;

namespace DjPortalApi.Features.Events;

public sealed class UpdateEventModel : CreateEventModel, IValidatableObject
{
    [Required]
    public string? Id { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Guid.TryParse(Id, out _))
        {
            yield return new ValidationResult("Invalid Id", [nameof(Id)]);
        }
    }
}