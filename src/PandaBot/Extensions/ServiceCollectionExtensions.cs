using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using DiscordBot.Models;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
<<<<<<< HEAD
using Microsoft.Extensions.Logging;
using SupabaseClient = Supabase.Client;
=======
>>>>>>> 0a7330edd6bdef4e16b484716a5c6340f9439482

namespace DiscordBot.Extensions;

/// <summary>
/// Extension methods for registering Discord bot services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Discord bot configuration, client, and related services into the DI container.
    /// </summary>
    public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
    {
        // Load bot configuration
        var botConfig = configuration.GetSection("Bot").Get<BotConfig>() ?? new BotConfig();
        services.AddSingleton(botConfig);

        // Configure and register Discord client
        var socketConfig = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
<<<<<<< HEAD
            GatewayIntents = GatewayIntents.Guilds | 
                             GatewayIntents.GuildMembers | 
                             GatewayIntents.GuildMessages |
                             GatewayIntents.GuildMessageReactions,
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100,
            AlwaysResolveStickers = false,
            UseInteractionSnowflakeDate = true
=======
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Info
>>>>>>> 0a7330edd6bdef4e16b484716a5c6340f9439482
        };
        services.AddSingleton(socketConfig);
        services.AddSingleton<DiscordSocketClient>();

<<<<<<< HEAD
        // Add InteractionService (important for slash commands)
=======
        // Add InteractionService (quan trọng cho slash commands)
>>>>>>> 0a7330edd6bdef4e16b484716a5c6340f9439482
        services.AddSingleton(x =>
        {
            var client = x.GetRequiredService<DiscordSocketClient>();
            return new InteractionService(client);
        });

        // Core bot services
        services.AddSingleton<DiscordBotService>();
<<<<<<< HEAD
        
        // External API services
        services.AddHttpClient<AshesCodexService>();

        // Supabase configuration and service
        var supabaseConfig = configuration.GetSection("Supabase").Get<SupabaseConfig>() ?? new SupabaseConfig();
        services.AddSingleton(supabaseConfig);
        
        services.AddSingleton<SupabaseClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SupabaseClient>>();
            var config = sp.GetRequiredService<SupabaseConfig>();
            
            logger.LogInformation("Creating Supabase client with URL: {Url}", config.Url);
            
            var options = new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false
            };
            var client = new SupabaseClient(config.Url, config.Key, options);
            
            return client;
        });
        
        services.AddSingleton<SupabaseCodexService>();
=======
>>>>>>> 0a7330edd6bdef4e16b484716a5c6340f9439482

        return services;
    }
}
