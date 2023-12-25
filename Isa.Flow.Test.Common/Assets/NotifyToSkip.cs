using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Test.Common
{
    public class NotifyToSkip : IValidatableObject
    {
        public int Value { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return new List<ValidationResult>();
        }
    }
}
