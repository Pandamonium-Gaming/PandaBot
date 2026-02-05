using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Services.StarCitizen;

namespace PandaBot.Modules.StarCitizen;

[Discord.Interactions.Group("sc", "Star Citizen commands")]
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

    [SlashCommand("commodity", "Check UEX commodity prices")]
    public async Task CommodityCommand(
        [Discord.Interactions.Summary("name", "The name of the commodity to search for")] string commodityName)
    {
        await DeferAsync();

        var logger = Services.GetRequiredService<ILogger<StarCitizenModule>>();
        logger.LogInformation("User {UserId} searching for commodity: {CommodityName}", Context.User.Id, commodityName);

        try
        {
            var uexService = Services.GetRequiredService<UEXCommodityService>();
            var embed = await uexService.GetCommodityPricesEmbedAsync(commodityName);

            if (embed == null)
            {
                await FollowupAsync($"❌ Could not find commodity '{commodityName}'. Please check the spelling and try again.");
                return;
            }

            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching commodity data for: {CommodityName}", commodityName);
            await FollowupAsync($"❌ Error fetching commodity data: {ex.Message}");
        }
    }
}
