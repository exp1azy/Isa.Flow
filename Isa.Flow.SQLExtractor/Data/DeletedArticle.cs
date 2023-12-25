using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Isa.Flow.SQLExtractor.Data
{
    /// <summary>
    /// Представление таблицы DeletedArticle в БД.
    /// </summary>
    [Table("DeletedArticle")]
    public class DeletedArticle
    {
        [Column("ID")] [Key] public int Id { get; set; }

        [Column("ArticleId")] public int ArticleId { get; set; }
    }
}