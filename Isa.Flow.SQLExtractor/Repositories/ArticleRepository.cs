using Isa.Flow.SQLExtractor.Data;
using Isa.Flow.SQLExtractor.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Isa.Flow.SQLExtractor.Repository
{
    /// <summary>
    /// Функционал взаимодействия с базой данных.
    /// </summary>
    public class ArticleRepository
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="config">Конфигурационная информация.</param>
        public ArticleRepository(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Метод чтения из БД указанного количества статей, идентификаторы которых больше указанного и меньше или равно указаного.
        /// </summary>
        /// <param name="fromId">Начало интервала.</param>
        /// <param name="toId">Конец интервала.</param>
        /// <param name="count">Кол-во статей.</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Задача, представляющая асинхронную операцию чтения списка статей.</returns>
        public async Task<List<Article>> GetArticlesFromIntervalAsync(int fromId, int toId, int count, CancellationToken cancellationToken)
        {
            using var context = new DataContext(_config);
            return await context.Articles.Include(a => a.Source)
                .Where(a => a.Id > fromId && a.Id <= toId).OrderBy(a => a.Id).Take(count)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Метод получения списка идентификаторов несуществующих в БД статей в указанном диапазоне.
        /// </summary>
        /// <param name="from">Начало диапазона идентификаторов.</param>
        /// <param name="to">Конец диапазона идентификаторов.</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Задача, представляющая асинхронную операцию чтения списка идентификаторов.</returns>
        public async Task<List<int>> GetNonExistentIdsFromIntervalAsync(int from, int to, CancellationToken cancellationToken)
        {
            using var context = new DataContext(_config);
            var nonExistentIds = context.Database.SqlQueryRaw<int>($"SELECT CAST(r.N AS int) as ArticleID FROM (SELECT TOP ({to}-{from}) {from}+row_number() over(order by t1.number) as N FROM master..spt_values t1 CROSS JOIN master..spt_values t2) AS r LEFT JOIN Article a ON a.ID = r.N WHERE a.ID is NULL");

            return await nonExistentIds.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Метод получения списка статей, у которых идентификатор больше указанного.
        /// </summary>
        /// <param name="amount">Количество запрашиваемых статей.</param>
        /// <param name="lastId">Значение идентиифкатора, после которого возвращаются статьи.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию чтения списка статей.</returns>
        public async Task<List<Article>?> GetArticlesWithOffsetAsync(int amount, int lastId, CancellationToken cancellationToken)
        {
            try
            {
                using var context = new DataContext(_config);
                return await context.Articles.Include(a => a.Source).OrderBy(a => a.Id).Where(a => a.Id > lastId).Take(amount).ToListAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(string.Format(Message.FailedReadingArticlesAttempt, e.ToString()));
                return null;
            }
        }

        /// <summary>
        /// Метод получения списка статей по указанному списку идентификаторов.
        /// </summary>
        /// <param name="ids">Список идентификатроов.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию чтения списка статей.</returns>
        public async Task<List<Article>> GetArticlesByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
        {
            using var context = new DataContext(_config);
            return await context.Articles.Include(a => a.Source).Where(a => ids.Contains(a.Id)).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Метод получения списка идентификаторов удаленных статей.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию чтения списка идентификаторов.</returns>
        public async Task<List<int>> GetDeletedArticlesAsync(CancellationToken cancellationToken)
        {
            using var context = new DataContext(_config);
            return await context.DeletedArticles.Select(d => d.ArticleId).Distinct().ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Метод получения списка идентификаторов измененных статей.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию чтения списка идентификаторов.</returns>
        public async Task<List<int>> GetUpdatedArticlesAsync(CancellationToken cancellationToken)
        {
            using var context = new DataContext(_config);
            return await context.UpdatedArticles.Select(u => u.ArticleId).Distinct().ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Метод удаления из таблицы UpdatedArticle указанных идентификаторов статей.
        /// </summary>
        /// <param name="updatedArticlesIds">Список идентификаторов.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию удаления.</returns>
        public async Task ClearRequestedUpdatedArticlesAsync(IEnumerable<int> updatedArticlesIds, CancellationToken cancellationToken)
        {
            var sql = $"delete from UpdatedArticle where ArticleID in ({string.Join(',', updatedArticlesIds.Select(i => i.ToString()))})";

            using var context = new DataContext(_config);

            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }

        /// <summary>
        /// Метод удаления из таблицы DeletedArticle указанных идентификаторов статей.
        /// </summary>
        /// <param name="ids">Список идентификаторов.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию удаления.</returns>
        public async Task ClearRequestedDeletedArticlesAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
        {
            var sql = $"delete from DeletedArticle where ArticleID in ({string.Join(',', ids.Select(i => i.ToString()))})";

            using var context = new DataContext(_config);

            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }
}