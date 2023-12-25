using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Entities
{
    /// <summary>
    /// Ответ на RPC-запрос в случае ошибки обработки запроса.
    /// </summary>
    public class RpcHandlingError : IValidatableObject
    {
        /// <summary>
        /// Сообщение об ошибке.
        /// </summary>
        [Required]
        public string? ErrorMessage { get; set; }

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