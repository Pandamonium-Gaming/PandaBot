using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBot.Core.Data;

namespace PandaBot.Core.Services;

public class DiscordBotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly CommandHandler _commandHandler;
    private readonly InteractionHandler _interactionHandler;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public DiscordBotService(
        DiscordSocketClient client,
        CommandHandler commandHandler,
        InteractionHandler interactionHandler,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _client = client;
        _commandHandler = commandHandler;
        _interactionHandler = interactionHandler;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Apply pending migrations
        await ApplyMigrationsAsync();

        var token = _configuration["Discord:Token"];
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Discord token not found in configuration");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Starting Discord Bot...");
        Console.ResetColor();

        await _commandHandler.InitializeAsync();
        await _interactionHandler.InitializeAsync();

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        
        // Wait for the Ready event to get the username
        var readyTcs = new TaskCompletionSource();
        Task ReadyHandler()
        {
            readyTcs.SetResult();
            return Task.CompletedTask;
        }
        
        _client.Ready += ReadyHandler;
        await readyTcs.Task;
        _client.Ready -= ReadyHandler;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Bot logged in as {_client.CurrentUser?.Username ?? "Unknown"}#{_client.CurrentUser?.Discriminator ?? "0000"}");
        Console.ResetColor();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Stopping Discord Bot...");
        Console.ResetColor();

        await _client.LogoutAsync();
        await _client.StopAsync();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Bot stopped successfully.");
        Console.ResetColor();
    }

    private async Task ApplyMigrationsAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Checking database migrations...");
        Console.ResetColor();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
        
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Applying {pendingMigrations.Count()} pending migration(s)...");
            Console.ResetColor();
            
            await dbContext.Database.MigrateAsync();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Migrations applied successfully!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Database is up to date.");
            Console.ResetColor();
        }
    }
}
