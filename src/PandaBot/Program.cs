using DiscordBot.Extensions;
using DiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using Serilog;
using System.Reflection;

// Get version
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

// Create logs directory
var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsDir);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(logsDir, "pandabot-.log"), 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("🤖 PandaBot v{Version} starting...", version);

try
{
    var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables("PANDABOT_");
                    config.AddUserSecrets<Program>();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDiscordBot(context.Configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                })
                .Build();

    // Run database migrations
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
        await db.Database.MigrateAsync();
    }

    var botService = host.Services.GetRequiredService<DiscordBotService>();

    // Start the host to run all hosted services
    await host.StartAsync();
    await botService.StartAsync();

    Log.Information("✅ PandaBot v{Version} is running", version);
    Console.WriteLine("Bot is running. Press CTRL+C to exit...");
    
    // Wait for shutdown signal (CTRL+C, SIGTERM, etc.)
    await host.WaitForShutdownAsync();

    await botService.StopAsync();
    await host.StopAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
