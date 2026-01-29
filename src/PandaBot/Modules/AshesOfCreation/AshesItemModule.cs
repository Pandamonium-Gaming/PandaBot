using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Services.AshesOfCreation;

namespace PandaBot.Modules.AshesOfCreation;

[Group("ashes", "Ashes of Creation commands")]
public class AshesItemModule : InteractionModuleBase<SocketInteractionContext>
{
    public IServiceProvider Services { get; set; } = null!;
    
    [SlashCommand("item", "Search for an item in Ashes of Creation")]
    public async Task ItemCommand(
        [Summary("name", "The name of the item to search for")] string name,
        [Summary("exact", "Whether to search for exact matches only")] bool exact = false)
    {
        await DeferAsync();
        
        var logger = Services.GetRequiredService<ILogger<AshesItemModule>>();
        logger.LogInformation("User {UserId} searching for item: '{ItemName}' (Exact: {Exact})", 
            Context.User.Id, name, exact);
        
        try
        {
            var itemService = Services.GetRequiredService<AshesItemService>();
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
            
            var results = await itemService.SearchItemsAsync(context, name, exact);

            logger.LogInformation("Search returned {Count} result(s)", results.Count);

            if (results.Count == 0)
            {
                await FollowupAsync($"No items found matching '{name}'.");
                return;
            }

            if (results.Count == 1)
            {
                var itemEmbed = await itemService.BuildItemEmbedAsync(context, results[0]);
                
                // Add debug button
                var components = new ComponentBuilder()
                    .WithButton("Show Debug Info", $"item_debug:{results[0].ItemId}", ButtonStyle.Secondary)
                    .Build();
                
                await FollowupAsync(embed: itemEmbed, components: components);
                return;
            }

            // Multiple results - show selection menu
            var selectMenu = new SelectMenuBuilder()
                .WithCustomId($"item_select:{Context.User.Id}")
                .WithPlaceholder($"Select an item ({results.Count} results)")
                .WithMinValues(1)
                .WithMaxValues(1);

            var displayCount = Math.Min(results.Count, 25);
            for (int i = 0; i < displayCount; i++)
            {
                var item = results[i];
                var label = item.Name.Length > 100 ? item.Name[..97] + "..." : item.Name;
                var description = $"{item.Type} - {item.Rarity}";
                if (description.Length > 100)
                    description = description[..97] + "...";
                
                selectMenu.AddOption(label: label, value: item.ItemId, description: description);
            }

            var component = new ComponentBuilder().WithSelectMenu(selectMenu).Build();

            var selectionEmbed = new EmbedBuilder()
                .WithTitle($"Found {results.Count} items matching '{name}'")
                .WithDescription(displayCount < results.Count 
                    ? $"Showing first {displayCount} results. Please refine your search."
                    : "Please select an item from the dropdown below:")
                .WithColor(Color.Blue)
                .Build();

            await FollowupAsync(embed: selectionEmbed, components: component);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing item search for '{ItemName}'", name);
            await FollowupAsync($"An error occurred while searching for '{name}': {ex.Message}");
        }
    }

    [ComponentInteraction("item_select:*", true)]
    public async Task HandleItemSelection(string userId, string[] selectedValues)
    {
        await DeferAsync();
        
        var logger = Services.GetRequiredService<ILogger<AshesItemModule>>();
        logger.LogInformation("HandleItemSelection - UserId: {UserId}, Values: {Values}", 
            userId, string.Join(", ", selectedValues));
        
        try
        {
            if (Context.User.Id.ToString() != userId)
            {
                await FollowupAsync("This selection menu is not for you.", ephemeral: true);
                return;
            }

            var itemService = Services.GetRequiredService<AshesItemService>();
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PandaBotContext>();

            var itemId = selectedValues[0];
            logger.LogInformation("Looking up item with ID: {ItemId}", itemId);
            
            var item = await itemService.GetItemByIdAsync(context, itemId);

            if (item == null)
            {
                await FollowupAsync("Item not found.");
                return;
            }

            logger.LogInformation("Raw JSON for {ItemName}: {RawJson}", item.Name, item.RawJson);

            var embed = await itemService.BuildItemEmbedAsync(context, item);
            
            // Add a debug button
            var components = new ComponentBuilder()
                .WithButton("Show Debug Info", $"item_debug:{itemId}", ButtonStyle.Secondary)
                .Build();
            
            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = embed;
                msg.Components = components;
            });
            
            logger.LogInformation("Successfully updated message");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in HandleItemSelection");
            await FollowupAsync($"An error occurred: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("item_debug:*", true)]
    public async Task HandleDebugButton(string itemId)
    {
        await DeferAsync(ephemeral: true);
        
        var logger = Services.GetRequiredService<ILogger<AshesItemModule>>();
        
        try
        {
            var itemService = Services.GetRequiredService<AshesItemService>();
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
            
            var item = await itemService.GetItemByIdAsync(context, itemId);

            if (item == null)
            {
                await FollowupAsync("Item not found.", ephemeral: true);
                return;
            }

            // Check recipes that use this item - exact match
            var recipeIngredients = await context.CachedRecipeIngredients
                .Where(ri => ri.ItemId == itemId)
                .Include(ri => ri.CachedCraftingRecipe)
                .ToListAsync();
            
            // Also check for partial matches to see if there's a formatting issue
            var partialMatches = await context.CachedRecipeIngredients
                .Where(ri => ri.ItemId.Contains(itemId) || itemId.Contains(ri.ItemId))
                .Take(5)
                .ToListAsync();
            
            // Get some random samples - load first then randomize client-side
            var randomSamples = (await context.CachedRecipeIngredients
                .Take(100)
                .ToListAsync())
                .OrderBy(x => Guid.NewGuid())
                .Take(5)
                .ToList();

            var recipeInfo = recipeIngredients.Any() 
                ? $"\n**✅ Used in {recipeIngredients.Count} recipe(s):**\n" + 
                  string.Join("\n", recipeIngredients.Take(5).Select(ri => $"• {ri.CachedCraftingRecipe?.Name ?? "Unknown"}"))
                : "\n**❌ Not found as ingredient in any recipes**";

            if (!recipeIngredients.Any() && partialMatches.Any())
            {
                recipeInfo += $"\n\n**⚠️ Partial matches found ({partialMatches.Count}):**\n" +
                    string.Join("\n", partialMatches.Select(pm => $"• ID: `{pm.ItemId}`"));
            }

            var debugInfo = $"**Item Data Dump for {item.Name}**\n\n" +
                $"**ItemId:** `{item.ItemId}`\n" +
                $"**Name:** {item.Name}\n" +
                $"**Views:** {item.Views:N0}\n" +
                recipeInfo + "\n\n" +
                $"**Database Stats:**\n" +
                $"• Total Recipes: {await context.CachedCraftingRecipes.CountAsync():N0}\n" +
                $"• Total Ingredients: {await context.CachedRecipeIngredients.CountAsync():N0}\n\n" +
                $"**Random Ingredient Samples (format check):**\n" +
                string.Join("\n", randomSamples.Select(s => $"• `{s.ItemId}` (Qty: {s.Quantity})"));

            await FollowupAsync(debugInfo, ephemeral: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in HandleDebugButton");
            await FollowupAsync($"An error occurred: {ex.Message}", ephemeral: true);
        }
    }
}
