using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Extractor.Models
{
    /// <summary>
    /// Модель статьи.
    /// </summary>
    public class ArticleModel : IValidatableObject
    {
        /// <summary>
        /// Идентификатор статьи.
        /// </summary>
        public int Id { get; set; } 

        /// <summary>
        /// Заголовок.
        /// </summary>
        public string Title { get; set; } 

        /// <summary>
        /// Дата публикации.
        /// </summary>
        public DateTime PubDate { get; set; } 

        /// <summary>
        /// Дата создания записи о статье.
        /// </summary>
        public DateTime Created { get; set; } 

        /// <summary>
        /// Идентификатор источника.
        /// </summary>
        public int SourceId { get; set; } 

        /// <summary>
        /// Текст статьи.
        /// </summary>
        public string? Body { get; set; } 

        /// <summary>
        /// Ссылка на статью.
        /// </summary>
        public string? Link { get; set; } 

        /// <summary>
        /// Модель источника.
        /// </summary>
        public SourceModel Source { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}