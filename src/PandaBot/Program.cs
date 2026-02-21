using DiscordBot.Extensions;
using DiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using Serilog;
using Serilog.Events;
using System.Reflection;

// Get version
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

// Create logs directory
var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsDir);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(logsDir, "pandabot-.log"), 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext:l} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("🤖 PandaBot v{Version} starting...", version);
Log.Information("Starting at {StartTime:o}", DateTime.UtcNow);

try
{
    Log.Information("Creating host builder...");
    var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    Log.Information("Configuring application settings for environment: {Environment}", context.HostingEnvironment.EnvironmentName);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables("PANDABOT_");
                    config.AddUserSecrets<Program>();
                })
                .ConfigureServices((context, services) =>
                {
                    Log.Information("Registering Discord bot services...");
                    services.AddDiscordBot(context.Configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                })
                .Build();

    Log.Information("Host builder created successfully");

    Log.Information("Running database migrations...");
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
        await db.Database.MigrateAsync();
    }
    Log.Information("Database migrations completed");

    Log.Information("Initializing UEX vehicle cache...");
    using (var scope = host.Services.CreateScope())
    {
        try
        {
            var uexService = scope.ServiceProvider.GetRequiredService<PandaBot.Services.StarCitizen.UEXVehicleService>();
            var cacheRefreshSuccess = await uexService.RefreshVehicleCacheAsync();
            if (cacheRefreshSuccess)
            {
                Log.Information("UEX vehicle cache initialized successfully");
            }
            else
            {
                Log.Warning("UEX vehicle cache initialization had issues, continuing startup");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to initialize UEX vehicle cache, continuing startup");
        }
    }
    Log.Information("Vehicle cache initialization completed");

    Log.Information("Initializing UEX item cache...");
    using (var scope = host.Services.CreateScope())
    {
        try
        {
            var uexItemService = scope.ServiceProvider.GetRequiredService<PandaBot.Services.StarCitizen.UEXItemService>();
            var itemCacheRefreshSuccess = await uexItemService.RefreshItemCacheAsync();
            if (itemCacheRefreshSuccess)
            {
                Log.Information("UEX item cache initialized successfully");
            }
            else
            {
                Log.Warning("UEX item cache initialization had issues, continuing startup");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to initialize UEX item cache, continuing startup");
        }
    }
    Log.Information("Item cache initialization completed");

    Log.Information("Getting DiscordBotService from DI container...");
    var botService = host.Services.GetRequiredService<DiscordBotService>();
    Log.Information("DiscordBotService retrieved successfully");

    Log.Information("Starting host (hosted services)...");
    await host.StartAsync();
    Log.Information("Host started successfully");

    Log.Information("Starting Discord bot service...");
    await botService.StartAsync();
    Log.Information("Discord bot service started successfully");

    Log.Information("✅ PandaBot v{Version} is running", version);
    Console.WriteLine("Bot is running. Press CTRL+C to exit...");
    
    // Wait for shutdown signal (CTRL+C, SIGTERM, etc.)
    await host.WaitForShutdownAsync();
    Log.Information("Shutdown signal received");

    Log.Information("Stopping bot service...");
    await botService.StopAsync();
    Log.Information("Bot service stopped");

    Log.Information("Stopping host...");
    await host.StopAsync();
    Log.Information("Host stopped");
}
catch (TaskCanceledException ex)
{
    Log.Fatal(ex, "⚠️ TASK CANCELLED during bot startup - this usually indicates a timeout or premature shutdown");
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
}
finally
{
    Log.Information("Closing Serilog...");
    await Log.CloseAndFlushAsync();
}

