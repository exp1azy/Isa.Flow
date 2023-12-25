using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Entities
{
    /// <summary>
    /// Пинг-сообщение, использующеяся в методе <see cref="BaseActor.PingAsync(string, int)"/>.
    /// </summary>
    public class Ping : IValidatableObject
    {
        /// <summary>
        /// Метка времени, когда было отправлено сообщение.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Метод валидации.
        /// </summary>
        /// <remarks>Представляет реализацию интерфеса <see cref="IValidatableObject"/>.</remarks>
        /// <param name="validationContext">Контекст валидации.</param>
        /// <returns>Список ошибок валидации.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            new List<ValidationResult>();
    }
}