using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.VkCollector
{
    public class VkCollectorResponse : IValidatableObject
    {
        public string? Response { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
