using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace PandaBot.Core.Services;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ILogger<InteractionHandler> _logger;

    public InteractionHandler(DiscordSocketClient client, InteractionService interactions, IServiceProvider services, ILogger<InteractionHandler> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        _client.InteractionCreated += HandleInteractionAsync;
        _client.Ready += ReadyAsync;
        
        _interactions.SlashCommandExecuted += SlashCommandExecuted;
        _interactions.ContextCommandExecuted += ContextCommandExecuted;
        _interactions.ComponentCommandExecuted += ComponentCommandExecuted;
    }

    private async Task ReadyAsync()
    {
        _logger.LogInformation("Ready event triggered, registering commands...");
        
        try
        {
#if DEBUG
            // In development, try guild registration first, fallback to global
            var guildId = 1032364488519987360ul;
            
            _logger.LogInformation("Attempting to register {Count} commands to guild {GuildId}", 
                _interactions.SlashCommands.Count, guildId);
            
            try
            {
                await _interactions.RegisterCommandsToGuildAsync(guildId);
                _logger.LogInformation("Successfully registered commands to guild {GuildId}", guildId);
            }
            catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Bot lacks permission to register to guild, falling back to global registration");
                await _interactions.RegisterCommandsGloballyAsync();
                _logger.LogInformation("Registered {Count} commands globally (will take up to 1 hour to appear)", 
                    _interactions.SlashCommands.Count);
            }
#else
            // In production, register globally
            _logger.LogInformation("Attempting to register {Count} commands globally", 
                _interactions.SlashCommands.Count);
            
            await _interactions.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Successfully registered commands globally");
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register commands");
        }
        
        // List all registered commands for debugging
        foreach (var cmd in _interactions.SlashCommands)
        {
            _logger.LogDebug("Registered command: {CommandName} in module {ModuleName}", 
                cmd.Name, cmd.Module.Name);
        }
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var receiveTime = DateTime.UtcNow;
            _logger.LogInformation("Received interaction type: {Type} at {Time}", 
                interaction.Type, receiveTime.ToString("HH:mm:ss.fff"));
            
            var context = new SocketInteractionContext(_client, interaction);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await _interactions.ExecuteCommandAsync(context, _services);
            sw.Stop();
            
            _logger.LogInformation("ExecuteCommandAsync completed in {Ms}ms, result: {Success}", 
                sw.ElapsedMilliseconds, result.IsSuccess);
            
            if (!result.IsSuccess)
            {
                _logger.LogError("Interaction execution failed: {Error}", result.ErrorReason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interaction");
            
            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                if (interaction.HasResponded)
                    await interaction.FollowupAsync("An error occurred while processing your command.", ephemeral: true);
                else
                    await interaction.RespondAsync("An error occurred while processing your command.", ephemeral: true);
            }
            else if (interaction.Type == InteractionType.MessageComponent)
            {
                if (interaction.HasResponded)
                    await interaction.FollowupAsync("An error occurred while processing your selection.", ephemeral: true);
                else
                    await interaction.RespondAsync("An error occurred while processing your selection.", ephemeral: true);
            }
        }
    }

    private Task SlashCommandExecuted(SlashCommandInfo command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            _logger.LogError("Slash command '{Command}' failed: {Error}. Error type: {ErrorType}", 
                command.Name, result.ErrorReason, result.Error);
            
            // If it's an exception result, log the exception details
            if (result is ExecuteResult executeResult && executeResult.Exception != null)
            {
                _logger.LogError(executeResult.Exception, "Exception during slash command execution");
            }
        }
        return Task.CompletedTask;
    }

    private Task ContextCommandExecuted(ContextCommandInfo command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            _logger.LogError("Context command '{Command}' failed: {Error}", command.Name, result.ErrorReason);
        }
        return Task.CompletedTask;
    }

    private Task ComponentCommandExecuted(ComponentCommandInfo command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            _logger.LogError("Component command '{Command}' failed: {Error}", 
                command?.Name ?? "Unknown", result.ErrorReason);
            
            // If it's an exception result, log the exception details
            if (result is ExecuteResult executeResult && executeResult.Exception != null)
            {
                _logger.LogError(executeResult.Exception, "Exception during component command execution");
            }
        }
        else
        {
            _logger.LogInformation("Component command '{Command}' executed successfully", 
                command?.Name ?? "Unknown");
        }
        return Task.CompletedTask;
    }
}
