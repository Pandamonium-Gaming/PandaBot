using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using DiscordBot.Models;
using Microsoft.Extensions.Logging;
using PandaBot;

namespace DiscordBot.Services;

/// <summary>
/// Main bot service that manages lifecycle events, logging, and command handling.
/// </summary>
public class DiscordBotService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly BotConfig _config;
    private readonly ILogger<DiscordBotService> _logger;

    public DiscordBotService(
        DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider services,
        BotConfig config,
        ILogger<DiscordBotService> logger)
    {
        _client = client;
        _interactionService = interactionService;
        _services = services;
        _config = config;
        _logger = logger;

        // Subscribe to Discord events
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.InteractionCreated += HandleInteractionAsync;
    }

    /// <summary>
    /// Starts the bot and connects to Discord.
    /// </summary>
    public async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.Token))
        {
            _logger.LogError("Bot token is not configured. Please set the token in appsettings.json");
            return;
        }

        // Login & connect
        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();

        // Load slash command modules
        await _interactionService.AddModulesAsync(typeof(Program).Assembly, _services);
    }

    /// <summary>
    /// Stops the bot and logs out from Discord.
    /// </summary>
    public async Task StopAsync()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    /// <summary>
    /// Handles log events from Discord.NET and maps them to Microsoft.Extensions.Logging levels.
    /// </summary>
    private Task LogAsync(LogMessage log)
    {
        var logLevel = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, log.Exception, "{Source}: {Message}", log.Source, log.Message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the bot has successfully connected and is ready.
    /// </summary>
    private async Task ReadyAsync()
    {
        _logger.LogInformation("Bot {Username} is connected and ready!", _client.CurrentUser.Username);

        if (_config.GuildId.HasValue)
        {
            await _interactionService.RegisterCommandsToGuildAsync(_config.GuildId.Value);
            _logger.LogInformation("Slash commands registered to guild {GuildId}", _config.GuildId.Value);
        }
        else
        {
            await _interactionService.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Slash commands registered globally");
        }
    }


    /// <summary>
    /// Handle slash command execution
    /// </summary>
    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, _services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interaction");

            if (interaction.Type == InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(async msg => await (await msg).DeleteAsync());
        }
    }
}
