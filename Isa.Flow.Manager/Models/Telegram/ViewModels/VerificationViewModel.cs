namespace Isa.Flow.Manager.Models.Telegram.ViewModels
{
    public class VerificationViewModel
    {
        /// <summary>
        /// Номер телефона.
        /// </summary>
        public string? Number { get; set; }

        /// <summary>
        /// Код верификации.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Сообщение об ошибке.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// true, если скачивание запущено, иначе, false
        /// </summary>
        public bool IsStarted { get; set; }
    }
}