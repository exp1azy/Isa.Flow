using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Extractor.Rpc
{
    /// <summary>
    /// Класс, необходимый для отправки запроса в RPC, который останавливает работу SQLExtractor.
    /// </summary>
    public class StopSqlExtractionRequest : IValidatableObject
    {
        /// <summary>
        /// Перечисление, сигнализирующее о том, какую функцию SQLExtractor необходимо остановить.
        /// </summary>
        public SqlExtractionFunc Func { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
