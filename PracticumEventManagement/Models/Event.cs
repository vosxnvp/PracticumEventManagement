using System.ComponentModel.DataAnnotations;

namespace PracticumEventManagement.Models
{
    public class Event : IValidatableObject
    {
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime StartAt { get; set; }

        [Required]
        public DateTime EndAt { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (EndAt <= StartAt)
            {
                yield return new ValidationResult(
                    "EndAt должен быть позже StartAt.",
                    new[] { nameof(EndAt) });
            }
        }
    }
}
