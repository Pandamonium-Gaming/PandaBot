using DiscordBot.Extensions;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddUserSecrets<Program>();
                config.AddEnvironmentVariables();
                var pandaBotToken = Environment.GetEnvironmentVariable("PandaBotToken");
                if (!string.IsNullOrEmpty(pandaBotToken))
                {
                    Console.WriteLine("Using PandaBotToken from environment variable.");
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Bot:Token"] = pandaBotToken
                    });
                }
                else
                {
                    Console.WriteLine("PandaBotToken environment variable not set. Using other configuration sources.");
                }
            })
            .ConfigureServices((context, services) =>
            {
                Console.WriteLine("Configuring services...");
                services.AddDiscordBot(context.Configuration);
                Console.WriteLine("Services configured successfully.");
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

Console.WriteLine("Building service provider...");

var botService = host.Services.GetRequiredService<DiscordBotService>();
Console.WriteLine("Service provider built, starting bot...");
await botService.StartAsync();

Console.WriteLine("Bot is running. Press any key to exit...");
Console.ReadKey();

await botService.StopAsync();
