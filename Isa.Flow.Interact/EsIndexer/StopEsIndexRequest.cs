using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.EsIndexer
{
    /// <summary>
    /// Класс запроса на остановку индексации.
    /// </summary>
    public class StopEsIndexRequest : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
