using Isa.Flow.EsIndexer.Resources;
using Isa.Flow.Interact.EsIndexer;
using Isa.Flow.Interact.Extractor.Rpc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isa.Flow.EsIndexer
{
    /// <summary>
    /// Параметры запуска функций инденксатора.
    /// </summary>
    internal class StartParams
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <remarks>Конструктор приватный. Для создания экземпляра объекта используйте метод <see cref="FromFile"/>.</remarks>
        private StartParams() { }

        /// <summary>
        /// Имя очереди, из которой принимать статьи для индексации.
        /// </summary>
        public string ArticlesQueue { get; set; }

        /// <summary>
        /// Имя очереди, из которой принимать статьи для удаления.
        /// </summary>
        public string DeleteQueue { get; set; }

        /// <summary>
        /// Метод установки параиметров запуска из запроса на запуск.
        /// </summary>
        /// <param name="request">Запрос на запуск.</param>
        public void Set(StartEsIndexRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.ArticlesQueue))
                ArticlesQueue = request.ArticlesQueue;

            if (!string.IsNullOrWhiteSpace(request.DeleteQueue))
                DeleteQueue = request.DeleteQueue;
        }

        /// <summary>
        /// Проверка параметров запуска функции и установка, если возможно, значений по умолчанию.
        /// </summary>
        /// <exception cref="ApplicationException">В случае, если не установлено имя очереди, из которой следует принимать статьи.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ArticlesQueue))
                throw new ApplicationException(Error.ArticlesQueueNotDefined);

            if (string.IsNullOrWhiteSpace(DeleteQueue))
                throw new ApplicationException(Error.DeleteQueueNotDefined);
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
                Log.Logger.Error(ex, Error.FuncParamsFileSaveError);
                return;
            }
        }

        /// <summary>
        /// Метод чтения параметров запуска из файла.
        /// </summary>
        /// <returns>Параметры запуска из файла или по умолчанию, если прочитать из файла по какой-либо причине не удалось.</returns>
        public static StartParams FromFile()
        {
            StartParams? inst = null;
            try
            {
                inst = JsonConvert.DeserializeObject<StartParams>(File.ReadAllText(_filename));
                Log.Logger.Information(Message.FuncParamsFileRead);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, Error.FuncParamsFileReadError);
                Log.Logger.Information(Message.DefaultFuncParamsSet);
            }

            return inst ?? new StartParams();
        }

        private static readonly string _filename = "esindexer.json";
    }
}