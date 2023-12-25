using Isa.Flow.Interact.Resources;
using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.TelegramCollector
{
    /// <summary>
    /// Класс, запрашивающий установку верификационного кода.
    /// </summary>
    public class SetTgCollectorVerificationRequest : IValidatableObject
    {
        public string VerificationCode { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VerificationCode) || VerificationCode == string.Empty)
            {
                yield return new ValidationResult(Error.VerificationCodeCannotBeNullEmptyOrBlank);
            }
        }
    }
}
