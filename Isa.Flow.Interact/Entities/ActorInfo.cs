using Isa.Flow.Interact.Resources;
using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Entities
{
    /// <summary>
    /// Информация об акторе.
    /// </summary>
    public class ActorInfo : IValidatableObject
    {
        /// <summary>
        /// Идентификатор актора.
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Тип актора
        /// </summary>
        /// <remarks>Полное текстовое обозначение типа, унаследованное от <seealso cref="BaseActor"/>, с указанием сборки.</remarks>
        [Required]
        public string Type { get; set; }

        /// <summary>
        /// Читабельное название типа актора (опционально).
        /// </summary>
        public string? DisplayType { get; set; }

        /// <summary>
        /// Описание актора (опционально).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Метод валидации объекта.
        /// </summary>
        /// <param name="validationContext">Контекст валидации.</param>
        /// <returns>Результаты валидации.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Id))
                yield return new ValidationResult(Error.ActorIdCannotBeNullEmptyOrBlank, new string[] { nameof(Id) });

            if (string.IsNullOrWhiteSpace(Type))
                yield return new ValidationResult(Error.ActorTypeCannotBeNullEmptyOrBlank, new string[] { nameof(Type) });
        }
    }
}