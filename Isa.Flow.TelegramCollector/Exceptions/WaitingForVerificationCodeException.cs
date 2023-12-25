using System.Runtime.Serialization;

namespace Isa.Flow.TelegramCollector.Exceptions
{
    public class WaitingForVerificationCodeException : Exception
    {
        public WaitingForVerificationCodeException()
        {
        }

        public WaitingForVerificationCodeException(string? message) : base(message)
        {
        }

        public WaitingForVerificationCodeException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected WaitingForVerificationCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}