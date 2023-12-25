using Elasticsearch.Net;
using Isa.Flow.EsIndexer.Dto;
using Microsoft.Extensions.Configuration;
using Nest;
using System.Configuration;
using Document = Isa.Flow.EsIndexer.Dto.Document;

namespace Isa.Flow.EsIndexer.Services
{
    /// <summary>
    /// Сервис для работы с ElasticSearch.
    /// </summary>
    public class ElasticService
    {
        private readonly string _indexPrefix;

        private readonly List<IndexDescription> _indices;

        private readonly ElasticClient _esClient;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="config">Конфигурация.</param>
        /// <param name="esClient">Клиент ElasticSearch.</param>
        /// <exception cref="ConfigurationErrorsException"></exception>
        public ElasticService(IConfiguration config, ElasticClient esClient)
        {
            _esClient = esClient;

            _indexPrefix = config.GetSection("ElasticSearch")["IndexPrefix"];
            if (string.IsNullOrWhiteSpace(_indexPrefix))
                throw new ConfigurationErrorsException(Resources.Error.ESIndexPrefixNotSpecifiedError);

            var request = new CatIndicesRequest(Indices.Index($"{_indexPrefix}*"));
            _indices = _esClient.Cat.Indices(request).Records
                .Select(i => (IndexDescription)i.Index)
                .Where(d => d.YearFrom != 0).ToList();
        }

        /// <summary>
        /// Метод получения статей по идентификаторам.
        /// </summary>
        /// <param name="ids">Список идентификаторов.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Список статей, полученных по указанным идентификаторам.</returns>
        public async Task<List<Document>> GetByIdAsync(List<long> ids, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ids == null || !ids.Any())
                    return new List<Document>();

                var distinctIds = ids.Distinct().ToList();

                var query = new IdsQuery
                {
                    Values = distinctIds.Select(id => new Id(id))
                };

                Func<SortDescriptor<Document>, IPromise<IList<ISort>>> sort = ss => ss
                    .Field(f => f
                        .Field(p => p.Document_id)
                        .Order(SortOrder.Ascending)
                        .UnmappedType(FieldType.Long)
                    );

                var result = new List<Document>();

                long totalSize = 0;
                var offset = 0;
                var batchSize = 1000;

                while (offset <= totalSize)
                {
                    var searchDescriptor = new SearchDescriptor<Document>()
                    .Index($"{_indexPrefix}*")
                    .Query(_ => query)
                    .SearchType(SearchType.QueryThenFetch)
                    .Sort(sort)
                    .From(offset)
                    .Size(batchSize);

                    var response = await _esClient.SearchAsync<Document>(_ => searchDescriptor, cancellationToken);

                    if (!response.ApiCall.Success)
                        throw response.ApiCall.OriginalException;

                    totalSize = response.Total;
                    if (totalSize == 0)
                        break;

                    result.AddRange(response.Hits.Select(h =>
                    {
                        h.Source.Index = h.Index;

                        var idx = _indices.FirstOrDefault(i => i.Name == h.Index) ?? (IndexDescription)h.Index;

                        h.Source.NeedsToMove = !(h.Source.Pubdate.Year >= idx.YearFrom && h.Source.Pubdate.Year <= idx.YearTo);

                        return h.Source;
                    }));

                    offset += batchSize;
                    if (offset > totalSize && response.Hits.Any())
                        totalSize = offset;
                }

                return result;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Метод, который проверяет нужно ли переместить документ в другой индекс.
        /// </summary>
        /// <param name="d">Документ.</param>
        /// <returns>Документ с обновленным свойством NeedsToMove.</returns>
        public Document SetNeedsToMove(Document d)
        {
            if (string.IsNullOrWhiteSpace(d.Index))
                return d;

            var idx = _indices.FirstOrDefault(i => i.Name == d.Index) ?? (IndexDescription)d.Index!;

            d.NeedsToMove = !(d.Pubdate.Year >= idx.YearFrom && d.Pubdate.Year <= idx.YearTo);

            return d;
        }

        /// <summary>
        /// Метод удаления списка документов из индекса.
        /// </summary>
        /// <param name="docs">Список документов.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns></returns>
        public async Task DeleteAsync(IEnumerable<Document> docs, CancellationToken cancellationToken = default)
        {
            if (docs == null || !docs.Any())
                return;

            var response = await _esClient.BulkAsync(descr => docs.Aggregate(descr, (d, i) => d.Delete<Document>(dd => dd.Index(i.Index).Id(i.Document_id))), cancellationToken);

            if (!response.ApiCall.Success)
                throw response.ApiCall.OriginalException;
        }

        /// <summary>
        /// Метод индексации списка документов.
        /// </summary>
        /// <param name="docs">Список документов.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns></returns>
        public async Task IndexAsync(IEnumerable<Document> docs, CancellationToken cancellationToken)
        {
            try
            {
                await _esClient.BulkAsync(descr =>
                    descr.IndexMany(docs, (bid, doc) =>
                    {
                        doc.Timestamp = DateTime.UtcNow;

                        var idx = _indices.FirstOrDefault(i => doc.Pubdate.Year >= i.YearFrom && doc.Pubdate.Year <= i.YearTo)?.Name
                            ?? $"{_indexPrefix}{doc.Pubdate.Year}";

                        return bid.Index(idx).Id(doc.Document_id);
                    }),
                    cancellationToken);
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}