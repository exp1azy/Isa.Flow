using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Extractor.Rpc
{
    public class ProgressResponse : IValidatableObject
    {
        public bool NewArticleProgress { get; set; }

        public bool ModifiedArticleProgress { get; set; }

        public bool DeletedArticleProgress { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
