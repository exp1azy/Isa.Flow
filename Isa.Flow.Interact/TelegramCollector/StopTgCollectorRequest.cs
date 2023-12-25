using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.TelegramCollector
{
    /// <summary>
    /// Класс, запрашивающий остановку TelegramCollector.
    /// </summary>
    public class StopTgCollectorRequest : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
