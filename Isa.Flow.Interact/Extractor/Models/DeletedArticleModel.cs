using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Extractor.Models
{
    /// <summary>
    /// Модель удаленной статьи.
    /// </summary>
    public class DeletedArticleModel : IValidatableObject
    {
        /// <summary>
        /// Идентификатор статьи.
        /// </summary>
        public int[] ArticleId;        

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
