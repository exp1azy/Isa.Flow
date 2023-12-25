using Isa.Flow.Interact;
using Isa.Flow.Interact.Extractor.Models;
using Isa.Flow.Interact.Extractor.Rpc;
using Isa.Flow.SQLExtractor.Data;
using Isa.Flow.SQLExtractor.Extensions;
using Isa.Flow.SQLExtractor.Repository;
using Isa.Flow.SQLExtractor.Resources;
using Serilog;

namespace Isa.Flow.SQLExtractor
{
    /// <summary>
    /// Реализация актора, выполняющего функции извлечения информации из БД.
    /// </summary>
    public partial class Extractor : BaseActor
    {
        /// <summary>
        /// Метод запуска указанной функции.
        /// </summary>
        /// <param name="func">Функция для запуска.</param>
        public void StartFunction(SqlExtractionFunc func)
        {
            _funcParams.Validate(func);

            if (func == SqlExtractionFunc.New)
            {
                if (_newTask == null || _newTask.IsCompleted)
                {
                    _cancellationTokenNew = new CancellationTokenSource();
                    _newTask = ExtractingNewArticlesAsync(_cancellationTokenNew.Token);
                }
            }
            else if (func == SqlExtractionFunc.Deleted)
            {
                if (_deletedTask == null || _deletedTask.IsCompleted)
                {
                    _cancellationTokenDeleted = new CancellationTokenSource();
                    _deletedTask = ExtractingDeletedArticlesAsync(_cancellationTokenDeleted.Token);
                }
            }
            else if (func == SqlExtractionFunc.Updated)
            {
                if (_modifiedTask == null || _modifiedTask.IsCompleted)
                {
                    _cancellationTokenModified = new CancellationTokenSource();
                    _modifiedTask = ExtractingModifiedArticlesAsync(_cancellationTokenModified.Token);
                }
            }
        }

        /// <summary>
        /// Метод запуска функции экстрактора.
        /// </summary>
        /// <param name="request">Запрос на запуск.</param>
        private void StartFunction(StartSqlExtractionRequest request)
        {
            _funcParams.Set(request);
            StartFunction(request.Func);
            _funcParams.ToFile();
        }

        /// <summary>
        /// Метод выполнения реиндексации.
        /// </summary>
        /// <param name="fromId">Идентификатор статьи с которого начинать реиндексацию.</param>
        /// <param name="toId">Идентификатор статьи на котором закончить реиндексацию.</param>
        /// <param name="queue">Название очереди, в которую помещать извлеченные статьи.</param>
        /// <returns>Задача, представляющая асинхронную операцию реиндексации.</returns>
        private async Task ReindexAsync(int fromId, int toId, string queue)
        {
            try
            {
                Log.Logger.Information(string.Format(Message.ReindexStarted, fromId, toId));

                var articleRepository = new ArticleRepository(_config);

                var from = fromId;

                while (from <= toId)
                {
                    var articles = await articleRepository.GetArticlesFromIntervalAsync(from, toId, _funcParams.NewArticleCount, default);
                    foreach (var article in articles)
                        Enqueue(queue, article.IntoModel()!);

                    from = articles.MaxBy(a => a.Id)?.Id ?? 0;

                    if (articles.Count > 0)
                        Log.Logger.Information(string.Format(Message.Reindexed, articles.Count, articles.MinBy(a => a.Id)?.Id ?? 0, from));

                    if (from == 0)
                        break;
                }

                Log.Logger.Information(Message.ReindexStopped);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, Message.ReindexStoppedWithError);
            }
        }

        /// <summary>
        /// Метод выполнения очистки.
        /// </summary>
        /// <param name="fromId">Идентификатор статьи с которого начинать очистку.</param>
        /// <param name="toId">Идентификатор статьи на котором закончить очистку.</param>
        /// <param name="queue">Название очереди, в которую помещать идентификаторы удалённых статей.</param>
        /// <returns>Задача, представляющая асинхронную операцию очистку.</returns>
        private async Task ClearAsync(int fromId, int toId, string queue)
        {
            try
            {
                Log.Logger.Information(string.Format(Message.CleanStarted, fromId, toId));

                var articleRepository = new ArticleRepository(_config);
                var deletedArticleModel = new DeletedArticleModel();

                var ids = await articleRepository.GetNonExistentIdsFromIntervalAsync(fromId, toId, default);
                for (int i = 0; i < ids.Count; i += _funcParams.DeletedArticleCount)
                {
                    var batch = ids.Skip(i).Take(_funcParams.DeletedArticleCount);

                    deletedArticleModel.ArticleId = batch.ToArray();

                    Enqueue(queue, deletedArticleModel);

                    Log.Logger.Information(string.Format(Message.Cleared, batch.Count(), batch.Min(), batch.Max()));
                }

                Log.Logger.Information(Message.CleanStopped);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, Message.CleanStoppedWithError);
            }
        }

        /// <summary>
        /// Метод, реализующий цикл извлечения новых статей.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая асинхронную операцию выполнения цикла извлечения.</returns>
        private async Task ExtractingNewArticlesAsync(CancellationToken cancellationToken)
        {
            Log.Logger.Information(Message.NewArticlesFuncStarted);

            Exception? error = null;

            do
            {
                List<Article> extracted = new();

                try
                {
                    List<Article>? articles;

                    while (true)
                    {
                        var articleRepository = new ArticleRepository(_config);
                        articles = await articleRepository.GetArticlesWithOffsetAsync(_funcParams.NewArticleCount, _funcParams.LastArticleId, cancellationToken);
                        if (articles != null)
                            break;
                        await Task.Delay(TimeSpan.FromSeconds(_funcParams.SleepIntervalAfterFailedAttempt), cancellationToken);                        
                    }

                    foreach (var article in articles)
                    {
                        while (!EnqueueArticle(article.IntoModel()!, _funcParams.NewArticleQueueName!))
                        {
                            if (extracted.Any())
                            {
                                var min = extracted.Min(r => r.Id);
                                var max = extracted.Max(r => r.Id);
                                _funcParams.LastArticleId = max;
                                _funcParams.ToFile();
                                Log.Logger.Information(string.Format(Message.NewExtracted, extracted.Count, min, max));
                                extracted.Clear();
                            }
                            await Task.Delay(TimeSpan.FromSeconds(_funcParams.SleepIntervalAfterFailedAttempt), cancellationToken);
                        }
                        extracted.Add(article);
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    if (extracted.Count > 0)
                    {
                        var min = extracted.Min(r => r.Id);
                        var max = extracted.Max(r => r.Id);
                        _funcParams.LastArticleId = max;
                        _funcParams.ToFile();
                        Log.Logger.Information(string.Format(Message.NewExtracted, extracted.Count, min, max));
                    }

                    try
                    {
                        if (error == null)
                            await Task.Delay(TimeSpan.FromSeconds(_funcParams.NewArticleIterationTimeout), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                }
            }
            while (error == null);

            if (error is OperationCanceledException)
                Log.Logger.Information(Message.NewArticlesFuncStopped);
            else
                Log.Logger.Error(error, Message.NewArticlesFuncStoppedWithError);
        }

        /// <summary>
        /// Метод помещения статьи в очередь.
        /// </summary>
        /// <param name="article">Модель статьи.</param>
        /// <returns>true, если удалось поместить статью в очередь, иначе - false.</returns>
        private bool EnqueueArticle(ArticleModel article, string queueName)
        {
            try
            {
                Enqueue(queueName, article);
                return true;
            }
            catch (Exception e)
            {
                Log.Logger.Warning(string.Format(Message.FailedSendingArticleAttempt, e.ToString()));
                return false;
            }
        }

        private bool EnqueueArticle(DeletedArticleModel deletedArticle)
        {
            try
            {
                Enqueue(_funcParams.DeletedArticleQueueName!, deletedArticle);
                return true;
            }
            catch (Exception e)
            {
                Log.Logger.Warning(string.Format(Message.FailedSendingArticleAttempt, e.ToString()));
                return false;
            }
        }

        /// <summary>
        /// Метод, реализующий функцию извлечения идентификаторов удаленных статей.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию извлечения идентификаторов удаленных статей.</returns>
        private async Task ExtractingDeletedArticlesAsync(CancellationToken cancellationToken)
        {
            Log.Logger.Information(Message.DeletedArticlesFuncStarted);

            Exception? error = null;

            do
            {
                try
                {
                    ArticleRepository articleRepository;
                    List<int> ids;

                    while (true)
                    {
                        articleRepository = new ArticleRepository(_config);
                        ids = await articleRepository.GetDeletedArticlesAsync(cancellationToken);
                        if (ids != null)
                            break;
                        await Task.Delay(TimeSpan.FromSeconds(_funcParams.SleepIntervalAfterFailedAttempt), cancellationToken);
                    }
                                               
                    for (int i = 0; i < ids.Count; i += _funcParams.DeletedArticleCount)
                    {
                        var batch = ids.Skip(i).Take(_funcParams.DeletedArticleCount);

                        while (!EnqueueArticle(new DeletedArticleModel { ArticleId = batch.ToArray() }))                       
                            await Task.Delay(TimeSpan.FromSeconds(_funcParams.SleepIntervalAfterFailedAttempt), cancellationToken);
                        
                        while (true)
                        {
                            try
                            {
                                await articleRepository.ClearRequestedDeletedArticlesAsync(batch, cancellationToken);
                                break;
                            }
                            catch 
                            {
                                await Task.Delay(TimeSpan.FromSeconds(_funcParams.SleepIntervalAfterFailedAttempt), cancellationToken);
                            }
                        }
                        
                        Log.Logger.Information(string.Format(Message.DeletedExtracted, batch.Count(), batch.Min(), batch.Max()));
                    }

                    await Task.Delay(_funcParams.DeletedArticleIterationTimeout * 1000, cancellationToken);                    
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            }
            while (error == null);

            if (error is OperationCanceledException)
                Log.Logger.Information(Message.DeletedArticlesFuncStopped);
            else
                Log.Logger.Error(error, Message.DeletedArticlesFuncStoppedWithError);
        }

        /// <summary>
        /// Метод, реализующий операцию извлечение измененных статей.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию извлечения измененных статей.</returns>
        private async Task ExtractingModifiedArticlesAsync(CancellationToken cancellationToken)
        {
            Log.Logger.Information(Message.UpdatedArticlesFuncStarted);

            Exception? error = null;

            do
            {
                try
                {
                    ArticleRepository articleRepository;
                    List<int> ids;

                    while (true)
                    {
                        articleRepository = new ArticleRepository(_config);
                        ids = await articleRepository.GetUpdatedArticlesAsync(cancellationToken);
                        if (ids != null)
                            break;
                        await Task.Delay(TimeSpan.FromSeconds(_funcParams.SleepIntervalAfterFailedAttempt), cancellationToken);
                    }
                    
                    for (int i = 0; i < ids.Count; i += 100)
                    {
                        var batch = ids.Skip(i).Take(100);

                        List<Article> articles;

                        while (true)
                        {
                            articles = await articleRepository.GetArticlesByIdsAsync(batch, cancellationToken);
                            if (articles != null)
                                break;
                            await Task.Delay(TimeSpan.FromSeconds(_funcParams.SleepIntervalAfterFailedAttempt), cancellationToken);
                        }
                        
                        var minId = int.MaxValue;
                        var maxId = 0;

                        foreach (var article in articles)
                        {
                            while (!EnqueueArticle(article.IntoModel()!, _funcParams.UpdatedArticleQueueName!))                           
                                await Task.Delay(TimeSpan.FromSeconds(_funcParams.SleepIntervalAfterFailedAttempt), cancellationToken);
                            
                            if (article.Id > maxId)
                                maxId = article.Id;

                            if (article.Id < minId)
                                minId = article.Id;
                        }

                        while (true)
                        {
                            try
                            {
                                await articleRepository.ClearRequestedUpdatedArticlesAsync(batch, cancellationToken);
                                break;
                            }
                            catch
                            {
                                await Task.Delay(TimeSpan.FromSeconds(_funcParams.SleepIntervalAfterFailedAttempt), cancellationToken);
                            }
                        }
                        
                        Log.Logger.Information(string.Format(Message.UpdatedExtracted, articles.Count, minId, maxId));
                    }

                    await Task.Delay(_funcParams.UpdatedArticleIterationTimeout * 1000, cancellationToken);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            }
            while (error == null);

            if (error is OperationCanceledException)
                Log.Logger.Information(Message.UpdatedArticlesFuncStopped);
            else
                Log.Logger.Error(error, Message.UpdatedArticlesFuncStoppedWithError);
        }
    }
}