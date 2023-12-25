using Isa.Flow.Interact.Resources;
using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Extractor.Rpc
{
    /// <summary>
    /// Класс запроса на переиндексацию интервала.
    /// </summary>
    public class IntervalRequest : IValidatableObject
    {
        /// <summary>
        /// Функция, необходимая для запуска конкретной функции.
        /// </summary>
        public SqlExtractionFunc Func { get; set; }

        /// <summary>
        /// Начальный идентификатор.
        /// </summary>
        public int From { get; set; }

        /// <summary>
        /// Конечный идентификатор.
        /// </summary>
        public int To { get; set; }

        /// <summary>
        /// Имя очереди.
        /// </summary>
        public string? Queue { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Func != SqlExtractionFunc.Reindex && Func != SqlExtractionFunc.Clean)
            {
                results.Add(new ValidationResult(Error.UnknownSqlExtractionIntervalFunc));
            }
            if (From > To)
            {
                results.Add(new ValidationResult(Error.FromToError));
            }

            return results;
        }
    }
}
