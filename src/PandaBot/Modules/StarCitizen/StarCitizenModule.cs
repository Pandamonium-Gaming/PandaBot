using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Services.StarCitizen;

namespace PandaBot.Modules.StarCitizen;

[Group("starcitizen", "Star Citizen commands")]
public class StarCitizenModule : InteractionModuleBase<SocketInteractionContext>
{
    public IServiceProvider Services { get; set; } = null!;

    [SlashCommand("status", "Check Star Citizen server status")]
    public async Task StatusCommand()
    {
        await DeferAsync();

        var logger = Services.GetRequiredService<ILogger<StarCitizenModule>>();
        logger.LogInformation("User {UserId} checking Star Citizen status", Context.User.Id);

        try
        {
            var statusService = Services.GetRequiredService<StarCitizenStatusService>();
            var embed = await statusService.GetStatusEmbedAsync();

            if (embed == null)
            {
                await FollowupAsync("❌ Failed to fetch Star Citizen status. Please try again later.");
                return;
            }

            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Star Citizen status");
            await FollowupAsync($"❌ Error fetching Star Citizen status: {ex.Message}");
        }
    }
}
