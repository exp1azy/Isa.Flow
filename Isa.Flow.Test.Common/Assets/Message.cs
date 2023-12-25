using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Test.Common
{
    public class Message : IValidatableObject
    {
        public string Text { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return new List<ValidationResult>();
        }
    }
}
