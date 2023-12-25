using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.VkCollector
{
    public class StartVkCollectorRequest : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
