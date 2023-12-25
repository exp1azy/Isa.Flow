using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.TelegramCollector
{
    /// <summary>
    /// Класс, возвращающий номер телефона, установленный в TelegramCollector.
    /// </summary>
    public class PhoneNumberResponse : IValidatableObject
    {
        public string PhoneNumber { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
