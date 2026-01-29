using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using DiscordBot.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Services;

public class DiscordBotService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly BotConfig _config;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly TaskCompletionSource<bool> _readyCompletionSource = new();

    public DateTime StartTime { get; private set; }

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

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.InteractionCreated += HandleInteractionAsync;
        _client.GuildAvailable += GuildAvailableAsync;
        _interactionService.Log += LogAsync;
        _interactionService.SlashCommandExecuted += SlashCommandExecutedAsync;
    }

    public async Task StartAsync()
    {
        StartTime = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(_config.Token))
        {
            _logger.LogError("Bot token is not configured. Please set the token using the environment variable, user secrets or appsettings.json. Exiting...");
            return;
        }

        _logger.LogInformation("Loading interaction service modules...");
        await _interactionService.AddModulesAsync(typeof(Program).Assembly, _services);
        
        var modules = _interactionService.Modules;
        _logger.LogInformation("Loaded {ModuleCount} modules:", modules.Count);
        foreach (var module in modules)
        {
            _logger.LogInformation("  Module: {ModuleName} with {CommandCount} slash commands, {ComponentCount} component commands", 
                module.Name, module.SlashCommands.Count, module.ComponentCommands.Count);
            foreach (var command in module.SlashCommands)
            {
                _logger.LogInformation("    - /{CommandName}: {Description}", command.Name, command.Description);
            }
            foreach (var component in module.ComponentCommands)
            {
                _logger.LogInformation("    - Component: {CustomId}", component.Name);
            }
        }

        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();

        _logger.LogInformation("Waiting for bot to be ready...");
        await _readyCompletionSource.Task;
        _logger.LogInformation("Bot is ready and guilds are cached");
    }

    public async Task StopAsync()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

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

    private Task ReadyAsync()
    {
        _logger.LogInformation("Bot {Username} is connected and ready!", _client.CurrentUser.Username);
        _logger.LogInformation("Client has {GuildCount} guilds in cache", _client.Guilds.Count);
        
        foreach (var guild in _client.Guilds)
        {
            _logger.LogInformation("Guild: {GuildName} ({GuildId})", guild.Name, guild.Id);
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            
            _logger.LogInformation("After delay, client has {GuildCount} guilds", _client.Guilds.Count);
            foreach (var guild in _client.Guilds)
            {
                _logger.LogInformation("Guild now available: {GuildName} ({GuildId})", guild.Name, guild.Id);
            }
            
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
            
            _readyCompletionSource.SetResult(true);
        });
        
        return Task.CompletedTask;
    }
    
    private Task GuildAvailableAsync(SocketGuild guild)
    {
        _logger.LogInformation("Guild available: {GuildName} ({GuildId})", guild.Name, guild.Id);
        return Task.CompletedTask;
    }

    private Task SlashCommandExecutedAsync(SlashCommandInfo command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            _logger.LogError("Slash command {CommandName} failed: {Error}", command.Name, result.ErrorReason);
        }
        else
        {
            _logger.LogInformation("Slash command {CommandName} executed successfully", command.Name);
        }
        return Task.CompletedTask;
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var receivedTime = DateTime.Now;
            var interactionCreatedAt = interaction.CreatedAt.UtcDateTime;
            var age = (receivedTime.ToUniversalTime() - interactionCreatedAt).TotalSeconds;
            
            _logger.LogInformation("[{Time}] Interaction {InteractionId} created at {CreatedAt}, age: {Age:F3}s, type: {Type}", 
                receivedTime.ToString("HH:mm:ss.fff"), interaction.Id, interactionCreatedAt.ToString("HH:mm:ss.fff"), age, interaction.Type);
            
            if (interaction is SocketMessageComponent component)
            {
                _logger.LogInformation("Component interaction with CustomId: '{CustomId}'", component.Data.CustomId);
            }
            
            var ctx = new SocketInteractionContext(_client, interaction);
            
            var beforeExecute = DateTime.Now;
            _logger.LogInformation("[{Time}] About to execute command (processing delay: {Delay}ms)", 
                beforeExecute.ToString("HH:mm:ss.fff"), (beforeExecute - receivedTime).TotalMilliseconds);
            
            var result = await _interactionService.ExecuteCommandAsync(ctx, _services);
            
            if (!result.IsSuccess)
            {
                _logger.LogError("Command execution failed: {Error}", result.ErrorReason);
            }
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
