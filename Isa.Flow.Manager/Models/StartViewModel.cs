namespace Isa.Flow.Manager.Models
{
    /// <summary>
    /// Модель, представляющая параметры запуска функции SQLExtractor.
    /// </summary>
    public class StartViewModel
    {
        /// <summary>
        /// Идентификатор статьи.
        /// </summary>
        public int? ArticleId { get; set; } = null;

        /// <summary>
        /// Функция.
        /// </summary>
        public int Func { get; set; }

        /// <summary>
        /// Начальный интервал.
        /// </summary>
        public int? From { get; set; } = null;

        /// <summary>
        /// Конечный интервал.
        /// </summary>
        public int? To { get; set; } = null;
    }
}
