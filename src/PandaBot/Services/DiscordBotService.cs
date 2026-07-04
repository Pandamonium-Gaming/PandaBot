using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using DiscordBot.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;

namespace DiscordBot.Services;

public class DiscordBotService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly BotConfig _config;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly SingleMessageService _singleMessageService;
    private readonly CrossChannelSpamDetector _spamDetector;
    private readonly AllCapsMessageModerator _allCapsModerator;
    private readonly CommandAccessService _commandAccessService;
    private readonly EventAuditLogService _eventAuditLogService;
    private readonly TaskCompletionSource<bool> _readyCompletionSource = new();
    private int _commandsRegistered;

    public DateTime StartTime { get; private set; }

    public DiscordBotService(
        DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider services,
        BotConfig config,
        ILogger<DiscordBotService> logger,
        SingleMessageService singleMessageService,
        CrossChannelSpamDetector spamDetector,
        AllCapsMessageModerator allCapsModerator,
        CommandAccessService commandAccessService,
        EventAuditLogService eventAuditLogService)
    {
        _client = client;
        _interactionService = interactionService;
        _services = services;
        _config = config;
        _logger = logger;
        _singleMessageService = singleMessageService;
        _spamDetector = spamDetector;
        _allCapsModerator = allCapsModerator;
        _commandAccessService = commandAccessService;
        _eventAuditLogService = eventAuditLogService;

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.Connected += ConnectedAsync;
        _client.Disconnected += DisconnectedAsync;
        _client.InteractionCreated += HandleInteractionAsync;
        _client.GuildAvailable += GuildAvailableAsync;
        _client.MessageReceived += message => RunMessageHandler(_eventAuditLogService.HandleMessageReceivedAsync, message, nameof(EventAuditLogService));
        _client.MessageReceived += message => RunMessageHandler(_singleMessageService.HandleMessageAsync, message, nameof(SingleMessageService));
        _client.MessageReceived += message => RunMessageHandler(_spamDetector.HandleMessageAsync, message, nameof(CrossChannelSpamDetector));
        _client.MessageReceived += message => RunMessageHandler(_allCapsModerator.HandleMessageAsync, message, nameof(AllCapsMessageModerator));
        _client.MessageDeleted += _eventAuditLogService.HandleMessageDeletedAsync;
        _client.UserJoined += _eventAuditLogService.HandleUserJoinedAsync;
        _client.UserLeft += _eventAuditLogService.HandleUserLeftAsync;
        _interactionService.Log += LogAsync;
        _interactionService.SlashCommandExecuted += SlashCommandExecutedAsync;
        _interactionService.ComponentCommandExecuted += ComponentCommandExecutedAsync;
    }

    // MessageReceived subscribers run sequentially and share one HandlerTimeout budget, so a slow
    // handler (multiple Discord API calls for delete/DM/mod-log) can block the gateway's dispatch
    // loop. Running each on its own background task keeps them independent of that shared budget.
    private Task RunMessageHandler(Func<SocketMessage, Task> handler, SocketMessage message, string handlerName)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await handler(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in {Handler} MessageReceived handler", handlerName);
            }
        });
        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        StartTime = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(_config.Token))
        {
            _logger.LogError("Bot token is not configured. Please set the token using the environment variable, user secrets or appsettings.json. Exiting...");
            return;
        }

        try
        {
            _logger.LogInformation("Loading Discord modules...");
            
            // Get all module types first to see what we're loading
            var moduleTypes = typeof(Program).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.BaseType?.Name == "InteractionModuleBase`1")
                .ToList();
            _logger.LogInformation("Found {ModuleCount} interaction modules: {Modules}", 
                moduleTypes.Count, string.Join(", ", moduleTypes.Select(t => t.Name)));
            
            // Add timeout to module loading to prevent hanging
            var moduleLoadingTask = _interactionService.AddModulesAsync(typeof(Program).Assembly, _services);
            var completedTask = await Task.WhenAny(moduleLoadingTask, Task.Delay(TimeSpan.FromSeconds(30)));
            
            if (completedTask != moduleLoadingTask)
            {
                _logger.LogError("Module loading timed out after 30 seconds. Modules found: {Modules}", 
                    string.Join(", ", moduleTypes.Select(t => t.Name)));
                throw new TimeoutException("Discord module loading exceeded 30 second timeout");
            }
            
            var loadedModuleCount = await moduleLoadingTask;
            _logger.LogInformation("Modules loaded successfully. Loaded {LoadedCount} modules", loadedModuleCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Discord modules. This may indicate a missing dependency or configuration issue. Details: {Message}", ex.Message);
            _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException?.Message);
            throw;
        }

        try
        {
            _logger.LogInformation("Logging in to Discord...");
            await _client.LoginAsync(TokenType.Bot, _config.Token);
            _logger.LogInformation("Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login to Discord");
            throw;
        }

        try
        {
            _logger.LogInformation("Starting Discord client...");
            await _client.StartAsync();
            _logger.LogInformation("Client started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Discord client");
            throw;
        }

        _logger.LogInformation("Waiting for bot to be ready...");
        var readyTask = _readyCompletionSource.Task;
        var completedReadyTask = await Task.WhenAny(readyTask, Task.Delay(TimeSpan.FromSeconds(60)));
        
        if (completedReadyTask != readyTask)
        {
            _logger.LogWarning("Bot ready signal not received within 60 seconds, continuing anyway");
        }
        else
        {
            _logger.LogInformation("Bot is ready");
        }
    }

    public async Task StopAsync()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private Task LogAsync(LogMessage log)
    {
        // Discord.Net can emit placeholder gateway warnings during reconnects and handshake churn.
        // These are not actionable and only add noise to the bot logs.
        if (ShouldSuppressGatewayNoise(log))
        {
            return Task.CompletedTask;
        }

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

        var message = string.IsNullOrWhiteSpace(log.Message) ? "(empty message)" : log.Message;
        _logger.Log(logLevel, log.Exception, "[{BotName}] {Source}: {Message}", "PandaBot", log.Source, message);
        return Task.CompletedTask;
    }

    private static bool ShouldSuppressGatewayNoise(LogMessage log)
    {
        if (string.IsNullOrWhiteSpace(log.Source) ||
            !log.Source.Contains("Gateway", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var message = log.Message?.Trim();
        return string.IsNullOrWhiteSpace(message) ||
               string.Equals(message, "null", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(message, "(empty message)", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(message, "(null)", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(message, "<null>", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(message, "[null]", StringComparison.OrdinalIgnoreCase);
    }

    private Task ReadyAsync()
    {
        _logger.LogInformation("Bot connected and ready");
        
        _ = Task.Run(async () =>
        {
            if (Interlocked.CompareExchange(ref _commandsRegistered, 1, 0) != 0)
            {
                _logger.LogDebug("[DiscordLifecycle] Skipping slash command re-registration on reconnect.");
                return;
            }

            try
            {
                await Task.Delay(2000);

                if (_client.ConnectionState != ConnectionState.Connected)
                {
                    Interlocked.Exchange(ref _commandsRegistered, 0);
                    _logger.LogInformation(
                        "Skipping slash command registration because client state is {State}. Will retry on next Ready.",
                        _client.ConnectionState);
                    return;
                }

                if (_config.GuildId.HasValue)
                {
                    _logger.LogInformation("Registering commands to guild...");
                    var commands = await _interactionService.RegisterCommandsToGuildAsync(_config.GuildId.Value);
                    _logger.LogInformation("Registered {CommandCount} commands", commands.Count);
                }
                else
                {
                    _logger.LogInformation("Registering commands globally...");
                    var commands = await _interactionService.RegisterCommandsGloballyAsync();
                    _logger.LogInformation("Registered {CommandCount} commands globally", commands.Count);
                }
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _commandsRegistered, 0);
                if (_client.ConnectionState != ConnectionState.Connected)
                {
                    _logger.LogInformation(
                        ex,
                        "Skipped slash command registration due to transient disconnect (state: {State}). Will retry on next Ready.",
                        _client.ConnectionState);
                }
                else
                {
                    _logger.LogError(ex, "Error registering commands");
                }
            }
            finally
            {
                _readyCompletionSource.TrySetResult(true);
            }
        });
        
        return Task.CompletedTask;
    }

    private Task ConnectedAsync()
    {
        _logger.LogInformation("[DiscordLifecycle] Connected to Discord as {Username} (state: {State})", _client.CurrentUser?.Username ?? "unknown", _client.ConnectionState);
        return Task.CompletedTask;
    }

    private Task DisconnectedAsync(Exception? ex)
    {
        if (ex is null)
        {
            _logger.LogInformation("[DiscordLifecycle] Disconnected from Discord (state: {State})", _client.ConnectionState);
        }
        else
        {
            _logger.LogInformation(
                "[DiscordLifecycle] Disconnected from Discord (state: {State}, reason: {Reason})",
                _client.ConnectionState,
                ex.Message);
        }

        return Task.CompletedTask;
    }

    private Task GuildAvailableAsync(SocketGuild guild)
    {
        _logger.LogInformation("Guild available: {GuildName} ({GuildId})", guild.Name, guild.Id);
        return Task.CompletedTask;
    }

    private async Task SlashCommandExecutedAsync(SlashCommandInfo? command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            // command is null when the interaction didn't match any registered command.
            _logger.LogError("Slash command {CommandName} failed: {Error}", command?.Name ?? "(unknown)", result.ErrorReason);
            var userMessage = BuildInteractionErrorMessage(result);
            if (context.Interaction is SocketInteraction socketInteraction)
                await SendInteractionErrorAsync(socketInteraction, userMessage);
        }
        else
        {
            _logger.LogInformation("Slash command {CommandName} executed successfully", command?.Name ?? "(unknown)");
        }
    }

    /// <summary>
    /// Called when a component (button/select menu) interaction is executed. Component command
    /// results are not surfaced anywhere else — without this, a failing handler is caught
    /// internally by ExecuteCommandAsync and silently discarded with no log and no user response.
    /// </summary>
    private async Task ComponentCommandExecutedAsync(ComponentCommandInfo? command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            // command is null when the custom ID didn't match any registered component command.
            _logger.LogError("Component command {CommandName} failed: {Error}", command?.Name ?? "(unknown)", result.ErrorReason);
            var userMessage = BuildInteractionErrorMessage(result);
            if (context.Interaction is SocketInteraction socketInteraction)
                await SendInteractionErrorAsync(socketInteraction, userMessage);
        }
        else
        {
            _logger.LogInformation("Component command {CommandName} executed successfully", command?.Name ?? "(unknown)");
        }
    }

    private async Task SendInteractionErrorAsync(SocketInteraction interaction, string message)
    {
        try
        {
            if (interaction.HasResponded)
                await interaction.FollowupAsync(message, ephemeral: true);
            else
                await interaction.RespondAsync(message, ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send interaction error message to user.");
        }
    }

    private static string BuildInteractionErrorMessage(IResult result)
    {
        if (result.Error == InteractionCommandError.UnmetPrecondition)
        {
            var reason = (result.ErrorReason ?? string.Empty).Trim();

            if (reason.Contains("too quickly", StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("cooldown", StringComparison.OrdinalIgnoreCase))
            {
                return $"⏱️ {reason}";
            }

            var normalized = reason.ToLower(CultureInfo.InvariantCulture);

            if (normalized.Contains("permission") || normalized.Contains("manage") || normalized.Contains("administrator"))
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return "❌ This command couldn't run because of missing permissions for you or the bot in this channel/server.";
                }

                return
                    "❌ This command couldn't run because of missing permissions for you or the bot in this channel/server." +
                    $"\nℹ️ Details: {reason}";
            }

            return $"❌ Command requirements not met: {reason}";
        }

        if (result.Error == InteractionCommandError.UnknownCommand)
            return "❌ I couldn't find that command. Please try again in a moment.";

        if (result.Error == InteractionCommandError.BadArgs)
            return "❌ Invalid command arguments. Please check the command options and try again.";

        if (result.Error == InteractionCommandError.Exception)
            return "❌ Something went wrong while running that command.";

        return $"❌ Command failed: {result.ErrorReason}";
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

            if (interaction is SocketSlashCommand slashCommand &&
                _commandAccessService.TryGetBlockReason(slashCommand.CommandName, interaction.Channel.Id, out var blockReason))
            {
                await interaction.RespondAsync($"❌ {blockReason}", ephemeral: true);
                return;
            }
            
            var ctx = new SocketInteractionContext(_client, interaction);
            
            var beforeExecute = DateTime.Now;
            _logger.LogInformation("[{Time}] About to execute command (processing delay: {Delay}ms)", 
                beforeExecute.ToString("HH:mm:ss.fff"), (beforeExecute - receivedTime).TotalMilliseconds);
            
            await _interactionService.ExecuteCommandAsync(ctx, _services);

            // Slash command and component command results (including precondition failures) are
            // handled in SlashCommandExecutedAsync/ComponentCommandExecutedAsync via their events.
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
