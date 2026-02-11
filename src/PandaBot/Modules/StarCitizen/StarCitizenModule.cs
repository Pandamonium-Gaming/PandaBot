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
            
            // Remove the dropdown message after selection
            try
            {
                await Context.Interaction.DeleteOriginalResponseAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete dropdown message");
            }
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
                var description = !string.IsNullOrWhiteSpace(vehicle.Manufacturer) 
                    ? vehicle.Manufacturer 
                    : "Unknown Manufacturer";
                
                selectMenuBuilder.AddOption(
                    label: $"{vehicle.Name} ({vehicle.Type})",
                    value: vehicle.UexVehicleId.ToString(),
                    description: description);
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
            
            // Remove the dropdown message after selection
            try
            {
                await Context.Interaction.DeleteOriginalResponseAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete dropdown message");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling vehicle selection");
            await FollowupAsync($"❌ Error fetching vehicle data: {ex.Message}", ephemeral: false);
        }
    }

    [SlashCommand("time", "Get local time for a Star Citizen location")]
    public async Task TimeCommand(
        [Discord.Interactions.Summary("location", "The name of the location (e.g., Lorville, Area18, New Babbage)")] string locationName)
    {
        await DeferAsync();

        var logger = Services.GetRequiredService<ILogger<StarCitizenModule>>();
        logger.LogInformation("User {UserId} checking time for location: {Location}", Context.User.Id, locationName);

        try
        {
            var verseTimeService = Services.GetRequiredService<VerseTimeService>();
            
            // Search for the location
            var locations = await verseTimeService.SearchLocationsAsync(locationName, maxResults: 5);
            
            if (locations.Count == 0)
            {
                await FollowupAsync($"❌ No locations found matching '{locationName}'. Try searching for landing zones like 'Lorville', 'Area18', or 'New Babbage'.");
                return;
            }
            
            // If exact match or single result, show time directly
            if (locations.Count == 1 || locations[0].Name.Equals(locationName, StringComparison.OrdinalIgnoreCase))
            {
                var timeInfo = await verseTimeService.GetLocationTimeAsync(locations[0].Name);
                
                if (timeInfo == null)
                {
                    await FollowupAsync($"❌ Failed to calculate time for {locations[0].Name}. The parent celestial body data may be unavailable.");
                    return;
                }
                
                var embed = new EmbedBuilder()
                    .WithTitle($"🕐 Local Time - {timeInfo.LocationName}")
                    .WithDescription($"**{timeInfo.LocalTimeFormatted}** ({timeInfo.IlluminationStatus})")
                    .AddField("Date", $"📅 {timeInfo.InGameDateString}", inline: false)
                    .AddField("Parent Body", timeInfo.ParentBody, inline: true)
                    .AddField("Parent Star", timeInfo.ParentStar, inline: true);
                
                // Add sunrise/sunset if available
                if (timeInfo.NextStarRise.HasValue)
                {
                    var sunriseTime = TimeSpan.FromMinutes(timeInfo.NextStarRise.Value);
                    embed.AddField("Next Sunrise", $"🌅 In {sunriseTime.Hours}h {sunriseTime.Minutes}m", inline: true);
                }
                if (timeInfo.NextStarSet.HasValue)
                {
                    var sunsetTime = TimeSpan.FromMinutes(timeInfo.NextStarSet.Value);
                    embed.AddField("Next Sunset", $"🌅 In {sunsetTime.Hours}h {sunsetTime.Minutes}m", inline: true);
                }
                
                embed.WithColor(GetIlluminationColor(timeInfo.IlluminationStatus))
                    .WithFooter($"Data from VerseTime • Time calculated at {DateTime.UtcNow:HH:mm:ss} UTC")
                    .WithCurrentTimestamp();
                
                await FollowupAsync(embed: embed.Build());
            }
            else
            {
                // Multiple results - show selection dropdown
                var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder("Select a location...")
                    .WithCustomId($"time_location_select:{Context.User.Id}")
                    .WithMinValues(1)
                    .WithMaxValues(1);
                
                for (int i = 0; i < Math.Min(5, locations.Count); i++)
                {
                    var loc = locations[i];
                    menuBuilder.AddOption(
                        label: loc.Name,
                        value: loc.Name,
                        description: $"{loc.Type} on {loc.ParentBody}"
                    );
                }
                
                var component = new ComponentBuilder()
                    .WithSelectMenu(menuBuilder)
                    .Build();
                
                var embed = new EmbedBuilder()
                    .WithTitle("🔍 Multiple Locations Found")
                    .WithDescription($"Found {locations.Count} locations matching '{locationName}'.\nPlease select one from the dropdown below:")
                    .WithColor(Discord.Color.Blue)
                    .WithFooter("This menu will expire in 2 minutes")
                    .Build();
                
                await FollowupAsync(embed: embed, components: component);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching location time");
            await FollowupAsync($"❌ Error fetching location time: {ex.Message}");
        }
    }

    private static Discord.Color GetIlluminationColor(string status)
    {
        return status switch
        {
            "Midnight" or "Night" => new Discord.Color(25, 25, 112), // Midnight blue
            "Morning Twilight" => new Discord.Color(135, 206, 250), // Light sky blue
            "Morning" or "Late Morning" => new Discord.Color(255, 215, 0), // Gold
            "Noon" => new Discord.Color(255, 255, 0), // Bright yellow
            "Afternoon" => new Discord.Color(255, 165, 0), // Orange
            "Evening" => new Discord.Color(255, 140, 0), // Dark orange
            "Evening Twilight" => new Discord.Color(70, 130, 180), // Steel blue
            _ => Discord.Color.DarkGrey
        };
    }

    [ComponentInteraction("time_location_select:*", true)]
    public async Task HandleLocationSelectAsync(string userId, string[] selectedValues)
    {
        // Verify the user who invoked the menu is the one interacting with it
        if (Context.User.Id.ToString() != userId)
        {
            await RespondAsync("❌ This menu is not for you!", ephemeral: true);
            return;
        }

        await DeferAsync();

        var locationName = selectedValues[0];
        var logger = Services.GetRequiredService<ILogger<StarCitizenModule>>();
        logger.LogInformation("User {UserId} selected location: {Location}", Context.User.Id, locationName);

        try
        {
            var verseTimeService = Services.GetRequiredService<VerseTimeService>();
            var timeInfo = await verseTimeService.GetLocationTimeAsync(locationName);

            if (timeInfo == null)
            {
                await FollowupAsync($"❌ Failed to calculate time for {locationName}. The parent celestial body data may be unavailable.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"🕐 Local Time - {timeInfo.LocationName}")
                .WithDescription($"**{timeInfo.LocalTimeFormatted}** ({timeInfo.IlluminationStatus})")
                .AddField("Date", $"📅 {timeInfo.InGameDateString}", inline: false)
                .AddField("Parent Body", timeInfo.ParentBody, inline: true)
                .AddField("Parent Star", timeInfo.ParentStar, inline: true);
            
            // Add sunrise/sunset if available
            if (timeInfo.NextStarRise.HasValue)
            {
                var sunriseTime = TimeSpan.FromMinutes(timeInfo.NextStarRise.Value);
                embed.AddField("Next Sunrise", $"🌅 In {sunriseTime.Hours}h {sunriseTime.Minutes}m", inline: true);
            }
            if (timeInfo.NextStarSet.HasValue)
            {
                var sunsetTime = TimeSpan.FromMinutes(timeInfo.NextStarSet.Value);
                embed.AddField("Next Sunset", $"🌅 In {sunsetTime.Hours}h {sunsetTime.Minutes}m", inline: true);
            }
            
            embed.WithColor(GetIlluminationColor(timeInfo.IlluminationStatus))
                .WithFooter($"Data from VerseTime • Time calculated at {DateTime.UtcNow:HH:mm:ss} UTC")
                .WithCurrentTimestamp();

            await FollowupAsync(embed: embed.Build());

            // Delete the original selection message
            try
            {
                var originalMessage = await Context.Interaction.GetOriginalResponseAsync();
                await originalMessage.DeleteAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete dropdown message");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching location time");
            await FollowupAsync($"❌ Error fetching location time: {ex.Message}");
        }
    }
}
