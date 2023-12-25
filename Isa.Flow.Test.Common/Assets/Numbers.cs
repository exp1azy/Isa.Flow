using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Test.Common
{
    public class Numbers : IValidatableObject
    {
        [Range(0, 3)]
        public int FirstNumber { get; set; } = 0;

        public int SecondNumber { get; set; } = 0;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return null;
        }
    }
}
