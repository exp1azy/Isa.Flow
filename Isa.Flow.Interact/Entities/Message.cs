using Isa.Flow.Interact.Resources;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Newtonsoft.Json;

namespace Isa.Flow.Interact.Entities
{
    /// <summary>
    /// Объект представляет сообщение, которыми обмениваются акторы.
    /// </summary>
    /// <remarks>Представляет собой "обёртку" для объекта полезной нагрузки, несущего содержание сообщения.</remarks>
    /// <typeparam name="TPayload">Тип, представляющий объект полезной нагрузки.</typeparam>
    public class Message<TPayload> : IValidatableObject
        where TPayload : IValidatableObject
    {
        /// <summary>
        /// Полное имя типа объекта, представляющего пролезную нагрузку.
        /// </summary>
        [Required]
        public string? Type { get; set; }

        /// <summary>
        /// Полезная нагрузка.
        /// </summary>
        [Required]
        public TPayload? Payload { get; set; }

        /// <summary>
        /// Метод сериализации объекта.
        /// </summary>
        /// <returns>Массив байтов, представляющих строку UTF-8, содержащую JSON-объект.</returns>
        /// <exception cref="SerializationException">В случае ошибки сериализации.</exception>
        public byte[] ToBytes()
        {
            if (Payload == null)
                throw new SerializationException(Error.NullPayload);

            try
            {
                Type = Payload.GetType().AssemblyQualifiedName;
                var serialized = JsonConvert.SerializeObject(this);
                return Encoding.UTF8.GetBytes(serialized);
            }
            catch (Exception ex)
            {
                throw new SerializationException(Error.SerializingError, ex);
            }
        }

        /// <summary>
        /// Метод десериализации объекта из массива байтов.
        /// </summary>
        /// <param name="bytes">Массив байтов, представляющих строку UTF-8, содержащую JSON-объект.</param>
        /// <returns>Десериализованый объект.</returns>
        /// <exception cref="SerializationException">В случае ошибки десериализации.</exception>
        public static Message<TPayload> FromBytes(byte[] bytes)
        {
            try
            {
                var msg = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<Message<TPayload>>(msg)!;
            }
            catch (Exception ex)
            {
                throw new SerializationException(Error.SerializingError, ex);
            }
        }

        /// <summary>
        /// Метод выбрасывает исключение в случае, если объект не проходит валидацию.
        /// </summary>
        /// <exception cref="ValidationException">В случае, если объект не валиден.</exception>
        public void ThrowIfInvalid()
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(this, new ValidationContext(this), results, true))
            {
                if (results.Any())
                    throw new ValidationException(results.First(), null, this);
                else
                    throw new ValidationException(Error.UnknownValidationError);
            }
        }

        /// <summary>
        /// Метод валидации сообщения.
        /// </summary>
        /// <param name="validationContext">Контекст валидации.</param>
        /// <returns>Список ошибок валидации.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type != Payload!.GetType().AssemblyQualifiedName)
                return new[] { new ValidationResult(Error.PayloadTypeMismatch, new[] { nameof(Payload) } ) };

            var results = new List<ValidationResult>();
            Validator.TryValidateObject(Payload!, new ValidationContext(Payload!), results, true);
            return results;
        }
    }
}