using Isa.Flow.Interact.Resources;
using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.TelegramCollector
{
    /// <summary>
    /// Класс, запрашивающий запуск TelegramCollector.
    /// </summary>
    public class StartTgCollectorRequest : IValidatableObject
    {
        public string Number { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Number) || Number == string.Empty)
            {
                yield return new ValidationResult(Error.NumberCannotBeNullEmptyOrBlank);
            }
        }
    }
}
