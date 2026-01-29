using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace PandaBot.Core.Services;

public class CommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services, IConfiguration configuration)
    {
        _client = client;
        _commands = commands;
        _services = services;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        _client.MessageReceived += HandleCommandAsync;
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message || message.Author.IsBot)
            return;

        int argPos = 0;
        var prefix = _configuration["Discord:Prefix"] ?? "!";

        if (!message.HasStringPrefix(prefix, ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            return;

        var context = new SocketCommandContext(_client, message);
        await _commands.ExecuteAsync(context, argPos, _services);
    }
}
