using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.EsIndexer
{
    /// <summary>
    /// Класс, возвращающий состояние индексатора.
    /// </summary>
    public class IndexerCurrentStateResponse : IValidatableObject
    {
        /// <summary>
        /// Свойство, описывающее время работы индексатора.
        /// </summary>
        public TimeSpan TimeSpan { get; set; }

        /// <summary>
        /// Свойство, описывающее состояние индексатора (запущен/остановлен).
        /// </summary>
        public bool IsStarted { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
