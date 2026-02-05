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
                await FollowupAsync($"❌ Could not find any items matching '{itemName}'. Please check the spelling and try again.", ephemeral: false);
                return;
            }

            // If only one match, fetch prices directly
            if (matches.Count == 1)
            {
                var match = matches[0];
                logger.LogInformation("Found single match for item: {ItemName} (ID: {ItemId})", match.Name, match.UexItemId);
                var embed = await uexService.GetItemPricesEmbedAsync(match.UexItemId);

                if (embed == null)
                {
                    await FollowupAsync($"❌ Could not fetch pricing data for '{match.Name}'. Please try again later.", ephemeral: false);
                    return;
                }

                await FollowupAsync(embed: embed, ephemeral: false);
                return;
            }

            // Multiple matches - show dropdown selection
            logger.LogInformation("Found {Count} matches for item search: {ItemName}", matches.Count, itemName);
            
            // Create select menu with options
            var selectMenuBuilder = new SelectMenuBuilder()
                .WithCustomId($"item_select:{Context.User.Id}")
                .WithPlaceholder("Select an item to view prices")
                .WithMinValues(1)
                .WithMaxValues(1);

            // Add options with just the IDs as values
            var optionCount = 0;
            foreach (var item in matches.Take(Math.Min(25, matches.Count)))
            {
                selectMenuBuilder.AddOption(
                    label: $"{item.Name} ({item.Category})",
                    value: item.UexItemId.ToString(),
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

            var selectionEmbed = new EmbedBuilder()
                .WithTitle($"Found {matches.Count} items matching '{itemName}'")
                .WithDescription("Please select an item from the dropdown below:")
                .WithColor(Color.Blue)
                .Build();

            logger.LogInformation("Sending select menu response with {MatchCount} matches", matches.Count);
            
            await FollowupAsync(embed: selectionEmbed, components: component, ephemeral: false);
            logger.LogInformation("Select menu response sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching for item: {ItemName}", itemName);
            await FollowupAsync($"❌ Error searching for item: {ex.Message}", ephemeral: false);
        }
    }

    [ComponentInteraction("item_select:*", true)]
    public async Task ItemSelectHandler(string userId, string[] values)
    {
        await DeferAsync();

        var logger = Services.GetRequiredService<ILogger<StarCitizenModule>>();
        
        try
        {
            if (Context.User.Id.ToString() != userId)
            {
                await FollowupAsync("This selection menu is not for you.", ephemeral: true);
                return;
            }

            if (!values.Any())
            {
                await FollowupAsync("❌ Invalid selection. Please try the search again.", ephemeral: false);
                return;
            }

            // Extract the item ID from the value
            var itemIdStr = values[0];

            if (!int.TryParse(itemIdStr, out var itemId))
            {
                await FollowupAsync("❌ Invalid item ID. Please try the search again.", ephemeral: false);
                return;
            }

            logger.LogInformation("User {UserId} selected item ID: {ItemId}", Context.User.Id, itemId);

            var uexService = Services.GetRequiredService<UEXItemService>();
            
            // Get the cached item to get its name
            var cachedItem = await uexService.GetCachedItemByIdAsync(itemId);
            if (cachedItem == null)
            {
                await FollowupAsync("❌ Item not found in cache. Please search again.", ephemeral: false);
                return;
            }

            // Fetch and display prices
            var embed = await uexService.GetItemPricesEmbedAsync(itemId);
            
            if (embed == null)
            {
                await FollowupAsync($"❌ Could not fetch pricing data for item ID {itemId}. Please try again later.", ephemeral: false);
                return;
            }

            await FollowupAsync(embed: embed, ephemeral: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling item selection");
            await FollowupAsync($"❌ Error fetching item data: {ex.Message}", ephemeral: false);
        }
    }

    [SlashCommand("vehicle", "Check UEX vehicle prices (search by name)")]
    public async Task VehicleCommand(
        [Discord.Interactions.Summary("name", "The name of the vehicle to search for")] string vehicleName)
    {
        await DeferAsync();

        var logger = Services.GetRequiredService<ILogger<StarCitizenModule>>();
        logger.LogInformation("User {UserId} searching for vehicle: {VehicleName}", Context.User.Id, vehicleName);

        try
        {
            var vehicleService = Services.GetRequiredService<UEXVehicleService>();
            
            // Perform fuzzy search
            var matches = await vehicleService.SearchVehiclesByNameFuzzyAsync(vehicleName, maxResults: 10);
            
            if (!matches.Any())
            {
                await FollowupAsync($"❌ Could not find any vehicles matching '{vehicleName}'. Please check the spelling and try again.", ephemeral: false);
                return;
            }

            // If only one match, fetch prices directly
            if (matches.Count == 1)
            {
                var match = matches[0];
                logger.LogInformation("Found single match for vehicle: {VehicleName} (ID: {VehicleId})", match.Name, match.UexVehicleId);
                var embed = await vehicleService.GetVehiclePricesEmbedAsync(match.UexVehicleId);

                if (embed == null)
                {
                    await FollowupAsync($"❌ Could not fetch pricing data for '{match.Name}'. Please try again later.", ephemeral: false);
                    return;
                }

                await FollowupAsync(embed: embed, ephemeral: false);
                return;
            }

            // Multiple matches - show dropdown selection
            logger.LogInformation("Found {Count} matches for vehicle search: {VehicleName}", matches.Count, vehicleName);
            
            // Create select menu with options
            var selectMenuBuilder = new SelectMenuBuilder()
                .WithCustomId($"vehicle_select:{Context.User.Id}")
                .WithPlaceholder("Select a vehicle to view prices")
                .WithMinValues(1)
                .WithMaxValues(1);

            // Add options with just the IDs as values
            var optionCount = 0;
            foreach (var vehicle in matches.Take(Math.Min(25, matches.Count)))
            {
                selectMenuBuilder.AddOption(
                    label: $"{vehicle.Name} ({vehicle.Type})",
                    value: vehicle.UexVehicleId.ToString(),
                    description: $"{vehicle.Manufacturer}");
                optionCount++;
                logger.LogDebug("Added option {OptionNum}: {VehicleName} (UexVehicleId: {UexVehicleId})", 
                    optionCount, vehicle.Name, vehicle.UexVehicleId);
            }

            logger.LogInformation("Building select menu with {OptionCount} options", optionCount);

            // Send message with select menu
            var component = new ComponentBuilder()
                .WithSelectMenu(selectMenuBuilder)
                .Build();

            var selectionEmbed = new EmbedBuilder()
                .WithTitle($"Found {matches.Count} vehicles matching '{vehicleName}'")
                .WithDescription("Please select a vehicle from the dropdown below:")
                .WithColor(Color.Blue)
                .Build();

            logger.LogInformation("Sending select menu response with {MatchCount} matches", matches.Count);
            
            await FollowupAsync(embed: selectionEmbed, components: component, ephemeral: false);
            logger.LogInformation("Select menu response sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching for vehicle: {VehicleName}", vehicleName);
            await FollowupAsync($"❌ Error searching for vehicle: {ex.Message}", ephemeral: false);
        }
    }

    [ComponentInteraction("vehicle_select:*", true)]
    public async Task VehicleSelectHandler(string userId, string[] values)
    {
        await DeferAsync();

        var logger = Services.GetRequiredService<ILogger<StarCitizenModule>>();
        
        try
        {
            if (Context.User.Id.ToString() != userId)
            {
                await FollowupAsync("This selection menu is not for you.", ephemeral: true);
                return;
            }

            if (!values.Any())
            {
                await FollowupAsync("❌ Invalid selection. Please try the search again.", ephemeral: false);
                return;
            }

            // Extract the vehicle ID from the value
            var vehicleIdStr = values[0];

            if (!int.TryParse(vehicleIdStr, out var vehicleId))
            {
                await FollowupAsync("❌ Invalid vehicle ID. Please try the search again.", ephemeral: false);
                return;
            }

            logger.LogInformation("User {UserId} selected vehicle ID: {VehicleId}", Context.User.Id, vehicleId);

            var vehicleService = Services.GetRequiredService<UEXVehicleService>();
            
            // Fetch and display prices
            var embed = await vehicleService.GetVehiclePricesEmbedAsync(vehicleId);
            
            if (embed == null)
            {
                await FollowupAsync($"❌ Could not fetch pricing data for vehicle ID {vehicleId}. Please try again later.", ephemeral: false);
                return;
            }

            await FollowupAsync(embed: embed, ephemeral: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling vehicle selection");
            await FollowupAsync($"❌ Error fetching vehicle data: {ex.Message}", ephemeral: false);
        }
    }
}
