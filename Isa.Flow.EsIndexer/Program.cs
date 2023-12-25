using Isa.Flow.EsIndexer.Resources;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Isa.Flow.EsIndexer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            EsIndexer esIndexer = null;

            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Logger(c => c.MinimumLevel.Verbose().WriteTo.Console())
                    .WriteTo.Logger(c => c.MinimumLevel.Error().WriteTo.File("error.log", rollingInterval: RollingInterval.Day))
                    .CreateLogger();
                Log.Logger.Information(Message.LoggingConfigured);

                IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("esindexer.conf.json", optional: false, reloadOnChange: true)
                .Build();
                Log.Logger.Information(Message.ConfigRead);

                var startParams = StartParams.FromFile();

                Log.Logger.Information(Message.ActorInitializing);
                Console.CursorTop--;
                esIndexer = new EsIndexer(startParams, config);
                Log.Logger.Information(string.Format(Message.ActorInitialized, esIndexer.Id));
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, Error.StartError);
            }

            if (args.Length == 1 && args.Contains("-start"))
            {
                try
                {
                    esIndexer!.StartFunctions();
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, Error.FuncStartError);
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