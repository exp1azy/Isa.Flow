using Isa.Flow.Interact.Extractor.Rpc;
using Isa.Flow.SQLExtractor.Resources;
using Newtonsoft.Json;
using Serilog;

namespace Isa.Flow.SQLExtractor
{
    /// <summary>
    /// Параметры запуска функций экстрактора.
    /// </summary>
    public class FuncParams
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <remarks>Конструктор приватный. Для создания экземпляра объекта используйте метод <see cref="FromFile"/>.</remarks>
        private FuncParams() { }

        /// <summary>
        /// Последний зафиксированный идентификатор статьи.
        /// </summary>
        public int LastArticleId { get; set; }

        /// <summary>
        /// Таймаут между итерациями для функции New.
        /// </summary>
        public int NewArticleIterationTimeout { get; set; }

        /// <summary>
        /// Количество статей, которые нужно получить для функции New.
        /// </summary>
        public int NewArticleCount { get; set; }

        /// <summary>
        /// Имя очереди для функции New.
        /// </summary>
        public string? NewArticleQueueName { get; set; }

        /// <summary>
        /// Таймаут между итерациями для функции Deleted.
        /// </summary>
        public int DeletedArticleIterationTimeout { get; set; }

        /// <summary>
        /// Количество статей, которые нужно получить для функции Deleted.
        /// </summary>
        public int DeletedArticleCount { get; set; }

        /// <summary>
        /// Имя очереди для функции Deleted.
        /// </summary>
        public string? DeletedArticleQueueName { get; set; }

        /// <summary>
        /// Таймаут между итерациями для функции Updated.
        /// </summary>
        public int UpdatedArticleIterationTimeout { get; set; }

        /// <summary>
        /// Имя очереди для функции Updated.
        /// </summary>
        public string? UpdatedArticleQueueName { get; set; }

        /// <summary>
        /// Ожидание в секундах после неудачной попытки (чтения из БД или помещения в очередь).
        /// </summary>
        public int SleepIntervalAfterFailedAttempt { get; set; }

        /// <summary>
        /// Метод установки параметров запуска из запроса на запуск функции.
        /// </summary>
        /// <param name="req">Запрос на запуск функции.</param>
        public void Set(StartSqlExtractionRequest req)
        {
            if (req.Func == SqlExtractionFunc.New)
            {
                if (req.LastArticleId > 0)
                    LastArticleId = req.LastArticleId;

                if (!string.IsNullOrWhiteSpace(req.QueueName))
                    NewArticleQueueName = req.QueueName;

                if (req.BatchSize > 0)
                    NewArticleCount = req.BatchSize;

                if (req.IterationTimeout > 0)
                    NewArticleIterationTimeout = req.IterationTimeout;
            }
            else if (req.Func == SqlExtractionFunc.Updated)
            {
                if (!string.IsNullOrWhiteSpace(req.QueueName))
                    UpdatedArticleQueueName = req.QueueName;

                if (req.IterationTimeout > 0)
                    UpdatedArticleIterationTimeout = req.IterationTimeout;
            }
            else if (req.Func == SqlExtractionFunc.Deleted)
            {
                if (!string.IsNullOrWhiteSpace(req.QueueName))
                    DeletedArticleQueueName = req.QueueName;

                if (req.BatchSize > 0)
                    DeletedArticleCount = req.BatchSize;

                if (req.IterationTimeout > 0)
                    DeletedArticleIterationTimeout = req.IterationTimeout;
            }
        }

        /// <summary>
        /// Проверка параметров запуска функции и установка, если возможно, значений по умолчанию.
        /// </summary>
        /// <param name="func">Функция, чьи параметры подлежат проверке.</param>
        /// <exception cref="ApplicationException">В случае, если не установлено имя очереди, в которую следует помещать результаты работы.</exception>
        public void Validate(SqlExtractionFunc func)
        {
            if (SleepIntervalAfterFailedAttempt < 0)
                SleepIntervalAfterFailedAttempt = 15;

            if (func == SqlExtractionFunc.New)
            {
                if (string.IsNullOrWhiteSpace(NewArticleQueueName))
                    throw new ApplicationException(Message.QueueNameNotDefined);

                if (LastArticleId < 0)
                    LastArticleId = 0;

                if (NewArticleCount < 1)
                    NewArticleCount = 100;

                if (NewArticleIterationTimeout < 1)
                    NewArticleIterationTimeout = 60;
            }
            if (func == SqlExtractionFunc.Updated)
            {
                if (string.IsNullOrWhiteSpace(UpdatedArticleQueueName))
                    throw new ApplicationException(Message.QueueNameNotDefined);

                if (UpdatedArticleIterationTimeout < 1)
                    UpdatedArticleIterationTimeout = 75;
            }
            if (func == SqlExtractionFunc.Deleted)
            {
                if (string.IsNullOrWhiteSpace(DeletedArticleQueueName))
                    throw new ApplicationException(Message.QueueNameNotDefined);

                if (DeletedArticleCount < 1)
                    DeletedArticleCount = 100;

                if (DeletedArticleIterationTimeout < 1)
                    DeletedArticleIterationTimeout = 90;
            }
        }

        /// <summary>
        /// Метод сохранения параметров запуска в файл.
        /// </summary>
        public void ToFile()
        {
            try
            {
                File.WriteAllText(_filename, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, Message.FuncParamsFileSaveError);
                return;
            }
        }

        /// <summary>
        /// Метод чтения параметров запуска из файла.
        /// </summary>
        /// <returns>Параметры запуска из файла или по умолчанию, если прочитать из файла по какой-либо причине не удалось.</returns>
        public static FuncParams FromFile()
        {
            FuncParams? inst = null;
            try
            {
                inst = JsonConvert.DeserializeObject<FuncParams>(File.ReadAllText(_filename));
                Log.Logger.Information(Message.FuncParamsFileRead);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, Message.FuncParamsFileReadError);
                Log.Logger.Information(Message.DefaultFuncParamsSet);
            }

            return inst ?? new FuncParams();
        }

        private static readonly string _filename = "sqlextractor.json";
    }
}