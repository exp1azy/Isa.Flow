using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Extractor.Rpc
{
    public class FuncStateResponse : IValidatableObject
    {
        public bool NewState { get; set; }

        public bool ModifiedState { get; set; }

        public bool DeletedState { get; set; }

        public bool ReindexState { get; set; }

        public bool CleanState { get; set; }

        public int LastArticleId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Array.Empty<ValidationResult>();
    }
}
