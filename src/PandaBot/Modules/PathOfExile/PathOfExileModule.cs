using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Services.PathOfExile;

namespace PandaBot.Modules.PathOfExile;

[Discord.Interactions.Group("pathofexile", "Path of Exile commands")]
[Discord.Commands.Alias("poe")]
public class PathOfExileModule : InteractionModuleBase<SocketInteractionContext>
{
    public IServiceProvider Services { get; set; } = null!;

    [SlashCommand("status", "Check Path of Exile server status")]
    public async Task StatusCommand()
    {
        await DeferAsync();

        var logger = Services.GetRequiredService<ILogger<PathOfExileModule>>();
        logger.LogInformation("User {UserId} checking Path of Exile status", Context.User.Id);

        try
        {
            var statusService = Services.GetRequiredService<PathOfExileStatusService>();
            var embed = await statusService.GetStatusEmbedAsync();

            if (embed == null)
            {
                await FollowupAsync("❌ Failed to fetch Path of Exile status. Please try again later.");
                return;
            }

            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Path of Exile status");
            await FollowupAsync("❌ An error occurred while fetching Path of Exile status. Please try again later.");
        }
    }
}
