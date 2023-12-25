using Microsoft.EntityFrameworkCore;

namespace Isa.Flow.Manager.Data
{
    /// <summary>
    /// Контекст данных.
    /// </summary>
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        /// <summary>
        /// Таблица с источниками.
        /// </summary>
        public DbSet<SourceDao> Sources { get; set; }

        /// <summary>
        /// Таблица с постами.
        /// </summary>
        public DbSet<ArticleDao> Articles { get; set; }
    }
}
