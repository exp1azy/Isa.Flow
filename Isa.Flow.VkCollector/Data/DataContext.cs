using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Isa.Flow.VkCollector.Data
{
    public class DataContext : DbContext
    {
        private readonly IConfiguration _config;

        public DataContext(IConfiguration config)
        {
            _config = config;
        }

        public DbSet<ArticleDao> Article { get; set; }

        public DbSet<SourceDao> Source { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_config["ConnectionString"]);
        }
    }
}
