using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Isa.Flow.SQLExtractor.Data
{
    /// <summary>
    /// Контекст базы данных.
    /// </summary>
    public class DataContext : DbContext
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="config">Конфигурационная информация.</param>
        public DataContext(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Представление таблицы Article в БД.
        /// </summary>
        public DbSet<Article> Articles { get; set; }

        /// <summary>
        /// Представление таблицы UpdatedArticle в БД.
        /// </summary>
        public DbSet<UpdatedArticle> UpdatedArticles { get; set; }

        /// <summary>
        /// Представление таблицы DeletedArticle в БД.
        /// </summary>
        public DbSet<DeletedArticle> DeletedArticles { get; set; }

        /// <summary>
        /// Метод конфигурирования контекста БД.
        /// </summary>
        /// <param name="optionsBuilder">Объект для настройки контекста БД.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_config.GetSection("ConnectionStrings")["SqlServerConnection"]);
        }
    }
}