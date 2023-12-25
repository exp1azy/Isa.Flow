namespace Isa.Flow.TelegramCollector.Exceptions
{
    public class AlreadyStartedException : Exception
    {
        public AlreadyStartedException() : base() { } 

        public AlreadyStartedException(string? message) : base(message) { }

        public AlreadyStartedException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
