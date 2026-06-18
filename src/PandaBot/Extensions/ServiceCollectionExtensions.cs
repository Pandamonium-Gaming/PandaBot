using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using DiscordBot.Models;
using DiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Models;
using PandaBot.Services.AshesOfCreation;

namespace DiscordBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
    {
        var discordSection = configuration.GetSection("Discord");
        BotConfig botConfig;
        try
        {
            botConfig = discordSection.Get<BotConfig>() ?? new BotConfig();
        }
        catch (Exception ex)
        {
            var diagnostics = string.Join(
                ", ",
                [
                    $"Discord:GuildId='{discordSection["GuildId"] ?? "<null>"}'",
                    $"Discord:AllowPrefixCommands='{discordSection["AllowPrefixCommands"] ?? "<null>"}'",
                    $"Discord:Heartbeat:Enabled='{discordSection["Heartbeat:Enabled"] ?? "<null>"}'",
                    $"Discord:Heartbeat:IntervalSeconds='{discordSection["Heartbeat:IntervalSeconds"] ?? "<null>"}'",
                    $"Discord:Heartbeat:StartupDelaySeconds='{discordSection["Heartbeat:StartupDelaySeconds"] ?? "<null>"}'",
                    $"Discord:Heartbeat:TimeoutSeconds='{discordSection["Heartbeat:TimeoutSeconds"] ?? "<null>"}'"
                ]);

            throw new InvalidOperationException(
                $"Failed to bind 'Discord' configuration. Check numeric/boolean environment values. {diagnostics}",
                ex);
        }

        // Validate bound configuration values
        botConfig.Validate();

        services.AddSingleton(botConfig);

        var modLogConfig = configuration.GetSection("ModerationLog").Get<ModerationLogConfig>() ?? new ModerationLogConfig();
        services.AddSingleton(modLogConfig);

        var spamConfig = configuration.GetSection("CrossChannelSpam").Get<CrossChannelSpamConfig>() ?? new CrossChannelSpamConfig();
        services.AddSingleton(spamConfig);

        var moderationExemptionsConfig = configuration.GetSection("ModerationExemptions").Get<ModerationExemptionsConfig>() ?? new ModerationExemptionsConfig();
        services.AddSingleton(moderationExemptionsConfig);

        var commandAccessConfig = configuration.GetSection("CommandAccess").Get<CommandAccessConfig>() ?? new CommandAccessConfig();
        services.AddSingleton(commandAccessConfig);

        var socketConfig = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMembers |
                             GatewayIntents.GuildMessages |
                             GatewayIntents.GuildMessageReactions |
                             GatewayIntents.MessageContent,
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100,
            AlwaysResolveStickers = false,
            UseInteractionSnowflakeDate = true
        };
        services.AddSingleton(socketConfig);
        services.AddSingleton<DiscordSocketClient>();

        services.AddSingleton(x =>
        {
            var client = x.GetRequiredService<DiscordSocketClient>();
            return new InteractionService(client);
        });

        services.AddSingleton<DiscordBotService>();
        services.AddSingleton<CommandAccessService>();
        services.AddSingleton<ModerationExemptionService>();
        services.AddSingleton<SingleMessageService>();
        services.AddSingleton<ModerationLogService>();
        services.AddSingleton<CrossChannelSpamDetector>();
        services.AddHttpClient<HeartbeatMonitorService>();
        services.AddHostedService<HeartbeatMonitorService>();

        // Load game modules configuration
        var gameModulesConfig = configuration.GetSection("GameModules").Get<GameModulesConfig>() ?? new GameModulesConfig();
        services.AddSingleton(gameModulesConfig);

        // Register Ashes of Creation services (if enabled)
        if (gameModulesConfig.EnableAshesOfCreation)
        {
            // Register memory cache for AshesForge services
            services.AddMemoryCache();
            
            services.AddScoped<ImageCacheService>();
            services.AddScoped<AshesItemService>();
            services.AddScoped<AshesRecipeService>();
            
            // Register named HttpClient for AshesForge API
            services.AddHttpClient("AshesForgeApi", client =>
            {
                client.BaseAddress = new Uri("https://www.ashesforge.com/api/");
                client.DefaultRequestHeaders.Add("User-Agent", "PandaBot/1.0");
            });
            
            services.AddScoped<AshesForgeApiService>();
            services.AddHostedService<AshesForgeDataCacheService>();
        }

        // Register Star Citizen services (if enabled)
        if (gameModulesConfig.EnableStarCitizen)
        {
            // Bind UEX API configuration
            services.Configure<UEXConfig>(configuration.GetSection("UEX"));
            
            // Add memory cache for UEX item/price caching and VerseTime location caching
            services.AddMemoryCache();

            services.AddHttpClient<PandaBot.Services.StarCitizen.StarCitizenStatusService>(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "PandaBot/1.0");
            });
            services.AddHttpClient<PandaBot.Services.StarCitizen.UEXCommodityService>(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "PandaBot/1.0");
            });
            services.AddHttpClient<PandaBot.Services.StarCitizen.UEXItemService>(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "PandaBot/1.0");
            });
            services.AddHttpClient<PandaBot.Services.StarCitizen.UEXVehicleService>(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "PandaBot/1.0");
            });
            services.AddHttpClient<PandaBot.Services.StarCitizen.VerseTimeService>(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "PandaBot/1.0");
            });

            // Register hosted services to initialize caches on startup
            services.AddHostedService<PandaBot.Services.StarCitizen.UEXItemCacheInitializerService>();
            services.AddHostedService<PandaBot.Services.StarCitizen.UEXVehicleCacheInitializerService>();
        }

        // Register Path of Exile services (if enabled)
        if (gameModulesConfig.EnablePathOfExile)
        {
            services.AddHttpClient<PandaBot.Services.PathOfExile.PathOfExileStatusService>();
        }

        // Register Return of Reckoning services (if enabled)
        if (gameModulesConfig.EnableReturnOfReckoning)
        {
            services.AddHttpClient<PandaBot.Services.ReturnOfReckoning.RORStatusService>();
        }

        // Register EF Core DbContext for SQLite
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=./pandabot.db";
        services.AddDbContext<PandaBot.Core.Data.PandaBotContext>(options =>
            options.UseSqlite(connectionString));

        services.AddHostedService<DiscordBot.Services.MetricsHostedService>();
        services.AddHostedService<SingleMessageHistoryBackfillHostedService>();

        return services;
    }
}

