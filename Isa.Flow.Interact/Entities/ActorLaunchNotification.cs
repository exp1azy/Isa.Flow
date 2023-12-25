using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Entities
{
    /// <summary>
    /// Класс представляет сообщение, использующеяся в качестве оповещения о намерении стартовать на этапе запуска актора.
    /// </summary>
    public class ActorLaunchNotification : IValidatableObject
    {
        /// <summary>
        /// Идентификатор запуска.
        /// </summary>
        /// <remarks>Не путать с идентификатором актора.</remarks>
        public Guid LaunchId { get; set; }

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