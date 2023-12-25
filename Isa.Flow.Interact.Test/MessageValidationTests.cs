using Isa.Flow.Interact.Entities;
using Isa.Flow.Test.Common;
using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Interact.Test
{
    /// <summary>
    /// Тестирование валидации <see cref="Message{TPayload}"/>.
    /// </summary>
    [TestClass]
    public class MessageValidationTests
    {
        [TestMethod]
        [ExpectedException(typeof(ValidationException))]
        public void TypeRequiredTest()
        {
            new Message<Payload>
            {
                Payload = new Payload()
            }.ThrowIfInvalid();
        }

        [TestMethod]
        [ExpectedException(typeof(ValidationException))]
        public void PayloadRequiredTest()
        {
            new Message<Payload>
            {
                Type = typeof(Payload).AssemblyQualifiedName
            }.ThrowIfInvalid();
        }

        [TestMethod]
        [ExpectedException(typeof(ValidationException))]
        public void TypeMismatchTest()
        {
            new Message<Payload>
            {
                Type = typeof(DateTime).AssemblyQualifiedName,
                Payload = new Payload()
            }.ThrowIfInvalid();
        }

        [TestMethod]
        [ExpectedException(typeof(ValidationException))]
        public void InvalidPayloadTest()
        {
            new Message<Payload>
            {
                Type = typeof(Payload).AssemblyQualifiedName,
                Payload = new Payload
                {
                    Value = -1
                }
            }.ThrowIfInvalid();
        }

        [TestMethod]
        public void SuccessValidationTest()
        {
            new Message<Payload>
            {
                Type = typeof(Payload).AssemblyQualifiedName,
                Payload = new Payload
                {
                    Value = 1
                }
            }.ThrowIfInvalid();
        }
    }
}