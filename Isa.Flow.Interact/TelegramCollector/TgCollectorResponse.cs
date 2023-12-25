using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.TelegramCollector
{
    /// <summary>
    /// Класс, возвращающий состояние и сообщение.
    /// </summary>
    public class TgCollectorResponse : IValidatableObject
    {
        public string? Message { get; set; }

        public TgCollectorStatusCode Status { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
