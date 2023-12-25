using Isa.Flow.TelegramCollector.Resources;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Isa.Flow.TelegramCollector
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            TgCollector collector;

            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Logger(c => c.MinimumLevel.Verbose().WriteTo.Console())
                    .WriteTo.Logger(c => c.MinimumLevel.Error().WriteTo.File("error.log", rollingInterval: RollingInterval.Day))
                    .CreateLogger();
                Log.Logger.Information(Message.LoggingConfigured);

                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("tgcollector.conf.json", optional: false, reloadOnChange: true)
                    .Build();
                Log.Logger.Information(Message.ConfigRead);

                Log.Logger.Information(Message.ActorInitializing);
                Console.CursorTop--;
                collector = new TgCollector(config);
                Log.Logger.Information(string.Format(Message.ActorInitialized, collector.Id));
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
                    _ = collector.StartAsync();
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, Message.CollectorError);
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