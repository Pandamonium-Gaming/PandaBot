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
                // Support explicit PandaBotToken environment variable
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
                // Support explicit SupaBaseToken environment variable
                var supabaseToken = Environment.GetEnvironmentVariable("SupaBaseToken");
                if (!string.IsNullOrEmpty(supabaseToken))
                {
                    Console.WriteLine("Using SupaBaseToken from environment variable.");
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Supabase:Key"] = supabaseToken
                    });
                }
                else
                {
                    Console.WriteLine("SupabaseToken environment variable not set. Using other configuration sources.");
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

// Initialize Supabase client asynchronously before starting the bot
Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Initializing Supabase client...");
var startTime = DateTime.Now;
var supabaseClient = host.Services.GetRequiredService<Supabase.Client>();
await supabaseClient.InitializeAsync();
var elapsed = (DateTime.Now - startTime).TotalSeconds;
Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Supabase client initialized successfully (took {elapsed:F2} seconds)");

var botService = host.Services.GetRequiredService<DiscordBotService>();
Console.WriteLine("Service provider built, starting bot...");
await botService.StartAsync();

Console.WriteLine("Bot is running. Press any key to exit...");
Console.ReadKey();

await botService.StopAsync();
