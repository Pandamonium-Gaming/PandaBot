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

    [SlashCommand("item", "Check UEX item prices (search by name)")]
    public async Task ItemCommand(
        [Discord.Interactions.Summary("name", "The name of the item to search for")] string itemName)
    {
        await DeferAsync();

        var logger = Services.GetRequiredService<ILogger<StarCitizenModule>>();
        logger.LogInformation("User {UserId} searching for item: {ItemName}", Context.User.Id, itemName);

        try
        {
            var uexService = Services.GetRequiredService<UEXItemService>();
            
            // Perform fuzzy search
            var matches = await uexService.SearchItemsByNameFuzzyAsync(itemName, maxResults: 10);
            
            if (!matches.Any())
            {
                await FollowupAsync($"❌ Could not find any items matching '{itemName}'. Please check the spelling and try again.");
                return;
            }

            // If only one match, fetch prices directly
            if (matches.Count == 1)
            {
                var match = matches[0];
                logger.LogInformation("Found single match for item: {ItemName} (ID: {ItemId})", match.Name, match.UexItemId);
                var embed = await uexService.GetItemPricesEmbedAsync(match.Name);

                if (embed == null)
                {
                    await FollowupAsync($"❌ Could not fetch pricing data for '{match.Name}'. Please try again later.");
                    return;
                }

                await FollowupAsync(embed: embed);
                return;
            }

            // Multiple matches - show dropdown selection
            logger.LogInformation("Found {Count} matches for item search: {ItemName}", matches.Count, itemName);
            
            // Create select menu with options
            var selectMenuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Select an item to view prices")
                .WithCustomId("item_select_menu")
                .WithMinValues(1)
                .WithMaxValues(1);

            // Add options (limit to Discord's max of 25 options)
            var optionCount = 0;
            foreach (var item in matches.Take(Math.Min(25, matches.Count)))
            {
                selectMenuBuilder.AddOption(
                    label: $"{item.Name} ({item.Category})",
                    value: $"item_select_{item.UexItemId}",
                    description: $"ID: {item.UexItemId}");
                optionCount++;
                logger.LogDebug("Added option {OptionNum}: {ItemName} (UexItemId: {UexItemId})", 
                    optionCount, item.Name, item.UexItemId);
            }

            logger.LogInformation("Building select menu with {OptionCount} options", optionCount);

            // Send message with select menu
            var component = new ComponentBuilder()
                .WithSelectMenu(selectMenuBuilder)
                .Build();

            logger.LogInformation("Sending select menu response with {MatchCount} matches", matches.Count);
            
            await FollowupAsync(
                text: $"Found **{matches.Count}** items matching '{itemName}'. Please select one:",
                components: component);
            logger.LogInformation("Select menu response sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching for item: {ItemName}", itemName);
            await FollowupAsync($"❌ Error searching for item: {ex.Message}");
        }
    }

    [ComponentInteraction("item_select_menu")]
    public async Task ItemSelectHandler(string[] values)
    {
        await DeferAsync();

        var logger = Services.GetRequiredService<ILogger<StarCitizenModule>>();
        
        try
        {
            if (!values.Any() || !values[0].StartsWith("item_select_"))
            {
                await FollowupAsync("❌ Invalid selection. Please try the search again.");
                return;
            }

            // Extract the item ID from the value (format: item_select_{UexItemId})
            var selectionValue = values[0];
            var itemIdStr = selectionValue.Replace("item_select_", "");

            if (!int.TryParse(itemIdStr, out var itemId))
            {
                await FollowupAsync("❌ Invalid item ID. Please try the search again.");
                return;
            }

            logger.LogInformation("User {UserId} selected item ID: {ItemId}", Context.User.Id, itemId);

            var uexService = Services.GetRequiredService<UEXItemService>();
            
            // Get the cached item to get its name
            var cachedItem = await uexService.GetCachedItemByIdAsync(itemId);
            if (cachedItem == null)
            {
                await FollowupAsync("❌ Item not found in cache. Please search again.");
                return;
            }

            // Fetch and display prices
            var embed = await uexService.GetItemPricesEmbedAsync(cachedItem.Name);
            
            if (embed == null)
            {
                await FollowupAsync($"❌ Could not fetch pricing data for '{cachedItem.Name}'. Please try again later.");
                return;
            }

            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling item selection");
            await FollowupAsync($"❌ Error fetching item data: {ex.Message}");
        }
    }
}
