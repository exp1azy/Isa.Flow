using Isa.Flow.Interact.Extractor.Models;
using Isa.Flow.SQLExtractor.Data;

namespace Isa.Flow.SQLExtractor.Extensions
{
    /// <summary>
    /// Методы расширения для типа <see cref="Article"/>.
    /// </summary>
    public static class ArticleExtension
    {
        /// <summary>
        /// Метод преобразования объекта типа <see cref="Article"/> в тип <see cref="ArticleModel"/>.
        /// </summary>
        /// <param name="article">Исходный объект.</param>
        /// <returns>Преобразованный объект.</returns>
        public static ArticleModel IntoModel(this Article article)
        {
            var srcType = article.Source?.Type?.ToLower()?.Trim() ?? string.Empty;
            var srcUrl = article.Source?.Site?.Trim() ?? string.Empty;
            var articleUrl = article.Link?.Trim() ?? string.Empty;

            return new ArticleModel
            {
                Source = new SourceModel
                {
                    Id = article.Source?.Id ?? 0,
                    Title = article.Source?.Title ?? string.Empty,
                    Site = srcType == "tg" ? $"https://t.me/{srcUrl}" : srcUrl
                },
                SourceId = article.SourceId,
                Body = article.Body,
                Created = article.Created,
                PubDate = article.PubDate,
                Id = article.Id,
                Link = srcType == "tg" ? $"https://t.me/{srcUrl}/{articleUrl}" : articleUrl,
                Title = article.Title.Normal(),
            };
        }
    }
}