using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.VkCollector
{
    public class VkCollectorCurrentStateResponse : IValidatableObject
    {
        public bool IsStarted { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
