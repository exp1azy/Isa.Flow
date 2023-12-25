using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.TelegramCollector
{
    /// <summary>
    /// Класс запроса на состояние работы TelegramCollector.
    /// </summary>
    public class CollectorCurrentStateRequest : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
