<<<<<<< HEAD
﻿using DiscordBot.Extensions;
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
=======
﻿using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Core.Services;
using PandaBot.Services.AshesOfCreation;

namespace PandaBot;

public class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables(prefix: "PANDABOT_");
                config.AddUserSecrets<Program>(optional: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Log configuration on startup
                LogConfiguration(context.Configuration);

                // Discord client configuration
                services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                    LogLevel = LogSeverity.Info
                }));

                // Command services
                services.AddSingleton<CommandService>();
                services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), new InteractionServiceConfig
                {
                    DefaultRunMode = Discord.Interactions.RunMode.Sync,
                    UseCompiledLambda = true
                }));

                // Database - use pooling for better performance
                services.AddDbContextPool<PandaBotContext>(options =>
                {
                    options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection") 
                        ?? "Data Source=pandabot.db");
#if DEBUG
                    // Suppress pending model changes warning in development
                    options.ConfigureWarnings(warnings => 
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
#endif
                });

                // Memory cache
                services.AddMemoryCache();

                // HTTP client
                services.AddHttpClient("AshesForge", client =>
                {
                    client.BaseAddress = new Uri("https://cdn.ashesforge.com");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                services.AddHttpClient("AshesForgeApi", client =>
                {
                    client.BaseAddress = new Uri("https://www.ashesforge.com/api/");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                // Core services
                services.AddSingleton<CommandHandler>();
                services.AddSingleton<InteractionHandler>();
                services.AddSingleton<LoggingService>();
                
                // Bot service
                services.AddHostedService<DiscordBotService>();
                services.AddHostedService<AshesForgeDataCacheService>(); // Renamed from ItemCacheBackgroundService

                // AoC services
                services.AddSingleton<ImageCacheService>();
                services.AddSingleton<AshesItemService>();
                services.AddTransient<AshesForgeApiService>();
            })
            .Build();

        await host.RunAsync();
    }

    private static void LogConfiguration(IConfiguration configuration)
    {
        var sensitiveKeys = new[] { "token", "password", "secret", "key", "connectionstring" };
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("PandaBot Configuration");
        Console.WriteLine("=".PadRight(80, '='));
        Console.ResetColor();

        Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}");
        Console.WriteLine($"Working Directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Discord Configuration:");
        Console.ResetColor();
        LogConfigValue(configuration, "Discord:Prefix", sensitiveKeys);
        LogConfigValue(configuration, "Discord:Token", sensitiveKeys, masked: true);
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Database Configuration:");
        Console.ResetColor();
        LogConfigValue(configuration, "ConnectionStrings:DefaultConnection", sensitiveKeys);
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("AshesForge Configuration:");
        Console.ResetColor();
        LogConfigValue(configuration, "AshesForge:CacheExpirationHours", sensitiveKeys);
        LogConfigValue(configuration, "AshesForge:EnableImageCaching", sensitiveKeys);
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Configuration loaded successfully!");
        Console.WriteLine("=".PadRight(80, '='));
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void LogConfigValue(IConfiguration configuration, string key, string[] sensitiveKeys, bool masked = false)
    {
        var value = configuration[key];
        var isSensitive = masked || sensitiveKeys.Any(sk => key.Contains(sk, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(value))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  {key}: <NOT SET>");
            Console.ResetColor();
        }
        else if (isSensitive)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  {key}: ********** (hidden)");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"  {key}: {value}");
        }
    }
}
>>>>>>> 0a7330edd6bdef4e16b484716a5c6340f9439482
