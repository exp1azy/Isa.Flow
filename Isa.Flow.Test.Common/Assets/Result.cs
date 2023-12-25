using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Test.Common
{
    public class Result : IValidatableObject
    {
        public int ResultNumber { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return null;
        }
    }
}
