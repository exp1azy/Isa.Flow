using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.EsIndexer
{
    /// <summary>
    /// Класс запроса на запуск индексации.
    /// </summary>
    public class StartEsIndexRequest : IValidatableObject
    {
        /// <summary>
        /// Имя очереди, из которой ждать новые и обновляемые статьи.
        /// </summary>
        public string? ArticlesQueue { get; set; }

        /// <summary>
        /// Имя очереди, из которых ждать id статей на удаление из ES.
        /// </summary>
        public string? DeleteQueue { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
