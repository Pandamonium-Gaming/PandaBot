using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Services.ReturnOfReckoning;

namespace PandaBot.Modules.ReturnOfReckoning;

/// <summary>
/// Discord commands for Return of Reckoning server status
/// </summary>
[Discord.Interactions.Group("returnofreckoning", "Return of Reckoning commands")]
[Discord.Commands.Alias("ror")]
public class RORModule : InteractionModuleBase<SocketInteractionContext>
{
    public IServiceProvider Services { get; set; } = null!;

    /// <summary>
    /// Check the current Return of Reckoning server status
    /// </summary>
    [SlashCommand("status", "Check Return of Reckoning server status")]
    public async Task CheckStatusAsync()
    {
        await DeferAsync();

        try
        {
            var logger = Services.GetRequiredService<ILogger<RORModule>>();
            var statusService = Services.GetRequiredService<RORStatusService>();

            logger.LogInformation("User {User} checking ROR status", Context.User.Username);

            var status = await statusService.GetServerStatusAsync();
            var embed = BuildStatusEmbed(status);

            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            var logger = Services.GetService<ILogger<RORModule>>();
            logger?.LogError(ex, "Error checking ROR status");
            await FollowupAsync("Error checking server status. Please try again later.");
        }
    }

    /// <summary>
    /// Builds a Discord embed with the server status
    /// </summary>
    private Embed BuildStatusEmbed(PandaBot.Models.ReturnOfReckoning.RORStatus status)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle("Return of Reckoning Server Status")
            .WithUrl("https://www.returnofreckoning.com/")
            .WithThumbnailUrl("https://www.returnofreckoning.com/images/logo.png")
            .WithTimestamp(status.LastChecked);

        if (status.IsOnline)
        {
            embedBuilder
                .WithColor(Color.Green)
                .AddField("Status", "🟢 Online", inline: true);
        }
        else
        {
            embedBuilder
                .WithColor(Color.Red)
                .AddField("Status", "🔴 Offline", inline: true);
        }

        if (status.PlayerCount > 0 || status.IsOnline)
        {
            var playerInfo = status.MaxPlayers.HasValue
                ? $"{status.PlayerCount} / {status.MaxPlayers}"
                : status.PlayerCount.ToString();

            embedBuilder.AddField("Players Online", playerInfo, inline: true);
        }

        if (!string.IsNullOrEmpty(status.StatusMessage))
        {
            embedBuilder.AddField("Message", status.StatusMessage, inline: false);
        }

        embedBuilder.WithFooter($"Last checked: {status.LastChecked:g}");

        return embedBuilder.Build();
    }
}
