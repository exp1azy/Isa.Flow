using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Extractor.Rpc
{
    public class FuncStatusRequest : IValidatableObject
    {
        public DateTime DateTime { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
