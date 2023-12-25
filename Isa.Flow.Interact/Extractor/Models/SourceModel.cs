namespace Isa.Flow.Interact.Extractor.Models
{
    /// <summary>
    /// Модель источника.
    /// </summary>
    public class SourceModel
    {
        /// <summary>
        /// Идентификатор источника.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название источника.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Ссылка на источник.
        /// </summary>
        public string Site { get; set; }
    }
}
