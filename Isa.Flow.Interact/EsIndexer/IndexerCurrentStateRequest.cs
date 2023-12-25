using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.EsIndexer
{
    /// <summary>
    /// Класс запроса на получение состояния индексатора.
    /// </summary>
    public class IndexerCurrentStateRequest : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
