using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Extractor.Rpc
{
    /// <summary>
    /// Класс, необходимый для получения ответа от RPC.
    /// </summary>
    public class SqlExtractionResponse : IValidatableObject
    {
        /// <summary>
        /// Время получения ответа.
        /// </summary>
        public DateTime DateTime { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
