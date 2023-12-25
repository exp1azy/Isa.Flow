using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Test.Common
{
    public class Payload : IValidatableObject
    {
        public int Value { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Value < 0)
                return new List<ValidationResult> { new ValidationResult(string.Empty, new[] { nameof(Value) }) };

            return new List<ValidationResult>();
        }
    }
}
