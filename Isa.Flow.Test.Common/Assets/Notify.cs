using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Test.Common
{
    public class Notify : IValidatableObject
    {
        public string Message { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return new List<ValidationResult>();
        }
    }
}
