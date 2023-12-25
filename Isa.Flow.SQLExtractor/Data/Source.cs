using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Isa.Flow.SQLExtractor.Data
{
    /// <summary>
    /// Представление таблицы Source в БД.
    /// </summary>
    [Table("Source")]
    public class Source
    {
        [Column("ID")][Key] public int Id { get; set; }

        public string Title { get; set; }

        public string? Site { get; set; }

        public string? Type { get; set; }
    }
}