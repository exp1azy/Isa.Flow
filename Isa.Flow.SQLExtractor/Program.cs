using Isa.Flow.SQLExtractor.Resources;
using Isa.Flow.Interact.Extractor.Rpc;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Isa.Flow.SQLExtractor
{
    /// <summary>
    /// Точка входа в программу.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Точка входа в программу.
        /// </summary>
        /// <param name="args">Аргументы командной строки. Параметр -start запускает все функции экстрактора.</param>
        public static void Main(string[] args)
        {
            Extractor extractor;

            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Logger(c => c.MinimumLevel.Verbose().WriteTo.Console())
                    .WriteTo.Logger(c => c.MinimumLevel.Error().WriteTo.File("error.log", rollingInterval: RollingInterval.Day))
                    .CreateLogger();
                Log.Logger.Information(Message.LoggingConfigured);

                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("sqlextractor.conf.json", optional: false, reloadOnChange: true)
                    .Build();
                Log.Logger.Information(Message.ConfigRead);
            
                var funcParams = FuncParams.FromFile();

                Log.Logger.Information(Message.ActorInitializing);
                Console.CursorTop--;
                extractor = new Extractor(funcParams, config);
                Log.Logger.Information(string.Format(Message.ActorInitialized, extractor.Id));
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, Message.StartError);
                return;
            }

            if (args.Length == 1 && args.Contains("-start"))
            {
                try
                {
                    extractor.StartFunction(SqlExtractionFunc.New);
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, Message.NewArticlesFuncStartError);
                }

                try
                {
                    extractor.StartFunction(SqlExtractionFunc.Deleted);
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, Message.DeletedArticlesFuncStartError);
                }

                try
                {
                    extractor.StartFunction(SqlExtractionFunc.Updated);
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, Message.UpdatedArticlesFuncStartError);
                }
            }

            Log.Logger.Information(Message.ExitPrompt);            

            string? exit;
            do
            {
                exit = Console.ReadLine()?.ToLower();
            }
            while (exit != "exit" && exit != "quit");            
        }
    }
}