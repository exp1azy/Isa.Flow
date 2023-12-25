using Isa.Flow.Interact.Resources;
using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Extractor.Rpc
{
    /// <summary>
    /// Класс, необходимый для отправки запроса в RPC, который запускает одну из функций SQLExtractor.
    /// </summary>
    public class StartSqlExtractionRequest : IValidatableObject
    {
        /// <summary>
        /// Перечисление, сигнализирующее о том, какую функцию SQLExtractor необходимо запустить.
        /// </summary>
        public SqlExtractionFunc Func { get; set; }

        /// <summary>
        /// Идентификатор последней зафиксированной статьи.
        /// </summary>
        public int LastArticleId { get; set; }

        /// <summary>
        /// Таймаут между итерациями.
        /// </summary>
        public int IterationTimeout { get; set; }

        /// <summary>
        /// Размер порции.
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// Имя очереди.
        /// </summary>
        public string QueueName { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Func != SqlExtractionFunc.New && Func != SqlExtractionFunc.Updated && Func != SqlExtractionFunc.Deleted)
            {
                results.Add(new ValidationResult(Error.UnknownSqlExtractionFunc));
            }
            if (LastArticleId < 0)
            {
                results.Add(new ValidationResult(Error.LastArticleIdCannotBeNegative));
            }

            return results;
        }
    }
}