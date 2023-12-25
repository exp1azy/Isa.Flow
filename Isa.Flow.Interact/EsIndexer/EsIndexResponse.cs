using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.EsIndexer
{
    /// <summary>
    /// Класс, возвращающий время ответа на запрос по индексатору.
    /// </summary>
    public class EsIndexResponse : IValidatableObject
    {
        /// <summary>
        /// Время ответа.
        /// </summary>
        public DateTime DateTime { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
