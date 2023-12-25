using Isa.Flow.Interact.Resources;
using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Entities
{
    /// <summary>
    /// Сообщение-ответ на пинг.
    /// </summary>
    public class Pong : IValidatableObject
    {
        /// <summary>
        /// Метка времени, когда был отправлен ответ.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Информация об акторе, отправившем ответ.
        /// </summary>
        [Required]
        public ActorInfo ActorInfo { get; set; }

        /// <summary>
        /// Метод валидации.
        /// </summary>
        /// <remarks>Представляет реализацию интерфеса <see cref="IValidatableObject"/>.</remarks>
        /// <param name="validationContext">Контекст валидации.</param>
        /// <returns>Список ошибок валидации.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ActorInfo == null)
                return new List<ValidationResult>() { new ValidationResult(Error.ActorInfoRequired, new string[] { nameof(ActorInfo) }) };

            var results = new List<ValidationResult>();
            Validator.TryValidateObject(ActorInfo!, new ValidationContext(ActorInfo!), results, true);
            return results;
        }
    }
}