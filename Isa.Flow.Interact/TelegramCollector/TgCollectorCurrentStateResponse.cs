using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.TelegramCollector
{
    /// <summary>
    /// Класс, возвращающий текущее состояние работы TelegramCollector.
    /// </summary>
    public class TgCollectorCurrentStateResponse : IValidatableObject
    {
        public bool IsStarted { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
