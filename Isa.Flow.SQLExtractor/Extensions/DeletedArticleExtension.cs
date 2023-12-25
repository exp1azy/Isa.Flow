using Isa.Flow.Interact.Extractor.Models;
using Isa.Flow.SQLExtractor.Data;

namespace Isa.Flow.SQLExtractor.Extensions
{
    /// <summary>
    /// Методы расширения для типа <see cref="IEnumerable<DeletedArticle>"/>.
    /// </summary>
    public static class DeletedArticleExtension
    {
        /// <summary>
        /// Метод преобразования коллекции типа <see cref="IEnumerable<DeletedArticle>"/> в тип <see cref="DeletedArticleModel"/>.
        /// </summary>
        /// <param name="articles">Исходная коллекция.</param>
        /// <returns>Преобразованный объект.</returns>
        public static DeletedArticleModel IntoModel(this IEnumerable<DeletedArticle> articles)
        {
            return new DeletedArticleModel
            {
                ArticleId = articles.Select(a => a.ArticleId).ToArray()
            };
        }
    }
}