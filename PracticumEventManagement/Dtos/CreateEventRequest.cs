using System.ComponentModel.DataAnnotations;

namespace PracticumEventManagement.Dtos;

public class CreateEventRequest : IValidatableObject
{
    [Required]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime? StartAt { get; set; }

    [Required]
    public DateTime? EndAt { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (StartAt.HasValue &&
            EndAt.HasValue &&
            EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "EndAt должен быть позже StartAt.",
                new[] { nameof(EndAt) });
        }
    }
}