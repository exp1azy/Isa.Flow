using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Isa.Flow.SQLExtractor.Extensions
{
    /// <summary>
    /// Функционал обработки текста статей.
    /// </summary>
    public static partial class StringExtensions
    {
        /// <summary>
        /// Метод нормализации строки.
        /// </summary>
        /// <param name="input">Строка для нормализации.</param>
        /// <returns>Нормализованная строка.</returns>
        public static string Normal(this string? input) =>
            string.IsNullOrWhiteSpace(input) ? string.Empty : regexSpaces().Replace(regexTags().Replace(input.Trim(), ""), " ");

        /// <summary>
        /// Метод создания хэшированного представления строки с помощью алгоритма SHA256.
        /// </summary>
        /// <param name="input">Строка, которую необходимо хэшировать.</param>
        /// <returns>Хэшированное представление указанной строки.</returns>
        public static string CreateSHA256(this string? input) =>
            string.IsNullOrWhiteSpace(input) ? string.Empty : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

        /// <summary>
        /// Регулярное выражение, представляющее последовательность пробельных символов различного типа.
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex("[\\s\\p{Z}\\p{Zs}\\p{Zl}\\p{Zp}]+")]
        private static partial Regex regexSpaces();

        /// <summary>
        /// Регулярное выражение, представляющее HTML-тег.
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex("<[\\s\\S]*?>")]
        private static partial Regex regexTags();
    }
}