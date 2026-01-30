using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using DiscordBot.Models;
using DiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Services.AshesOfCreation;

namespace DiscordBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
    {
        var botConfig = configuration.GetSection("Discord").Get<BotConfig>() ?? new BotConfig();
        services.AddSingleton(botConfig);

        var socketConfig = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.Guilds | 
                             GatewayIntents.GuildMembers | 
                             GatewayIntents.GuildMessages |
                             GatewayIntents.GuildMessageReactions,
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

        // Register memory cache for AshesForge services
        services.AddMemoryCache();

        // Register AshesForge services
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

        // Register EF Core DbContext for SQLite
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=./pandabot.db";
        services.AddDbContext<PandaBot.Core.Data.PandaBotContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}
