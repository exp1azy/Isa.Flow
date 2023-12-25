using Isa.Flow.Interact.Extractor.Models;
using Isa.Flow.SQLExtractor.Data;
using Nest;

namespace Isa.Flow.EsIndexer.Dto
{
    public class Document
    {
        /// <summary>
        /// Идентификатор статьи.
        /// </summary>
        public long Document_id { get; set; }

        /// <summary>
        /// Дата создания.
        /// </summary>
        public DateTime Createddate { get; set; }

        /// <summary>
        /// Дата публикации.
        /// </summary>
        public DateTime Pubdate { get; set; }

        /// <summary>
        /// Заголовок статьи.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Текст статьи.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Ссылка на статью.
        /// </summary>
        public string? Link_url { get; set; }

        /// <summary>
        /// Идентификатор источника.
        /// </summary>
        public int Source_id { get; set; }

        /// <summary>
        /// Название источника.
        /// </summary>
        public string? Source_title { get; set; }

        /// <summary>
        /// Ссылка на источник.
        /// </summary>
        public string? Site_url { get; set; }

        /// <summary>
        /// Дата/время индексации или обновления документа в индексе.
        /// </summary>
        [Date(Name = "@timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Версия документа.
        /// </summary>
        [Text(Name = "@version")]
        public string Version { get; set; } = "1";

        [Ignore]
        /// <summary>
        /// Наименование индекса, в котором найдена статья.
        /// </summary>
        public string? Index { get; set; }

        [Ignore]
        /// <summary>
        /// Признак того, что документ нуждается в перемещении в другой индекс.
        /// </summary>
        /// <remarks>Этот флаг означает то, что документ расположен не в том индексе, в котором должен быть, судя по дате публикации.
        /// Индексы организованы по годам публикаций и именованы соответственно.</remarks>
        public bool NeedsToMove { get; set; }

        ///// <summary>
        ///// Оператор приведения сущности типа <see cref="Article"/> к типу <see cref="Document"/>.
        ///// </summary>
        ///// <param name="article">Исходная сущность.</param>
        public static explicit operator Document(ArticleModel article) => new()
        {
            Document_id = article.Id,
            Createddate = article.Created,
            Pubdate = article.PubDate,
            Title = article.Title,
            Body = article.Body != null && article.Body.Length > 100000 ? article.Body.Remove(100000) : article.Body,
            Link_url = article.Link,
            Source_id = article.SourceId,
            Source_title = article.Source.Title,
            Site_url = article.Source.Site
        };
    }
}
