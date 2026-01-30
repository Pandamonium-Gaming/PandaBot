using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Services.AshesOfCreation;

namespace PandaBot.Modules.AshesOfCreation;

[Group("ashes", "Ashes of Creation commands")]
public class AshesModule : InteractionModuleBase<SocketInteractionContext>
{
    public IServiceProvider Services { get; set; } = null!;
    
    #region Item Command
    
    [SlashCommand("item", "Search for an item in Ashes of Creation")]
    public async Task ItemCommand(
        [Summary("name", "The name of the item to search for")] string name,
        [Summary("exact", "Whether to search for exact matches only")] bool exact = false)
    {
        await DeferAsync();
        
        var logger = Services.GetRequiredService<ILogger<AshesModule>>();
        logger.LogInformation("User {UserId} searching for item: '{ItemName}' (Exact: {Exact})", 
            Context.User.Id, name, exact);
        
        try
        {
            var itemService = Services.GetRequiredService<AshesItemService>();
            var apiService = Services.GetRequiredService<AshesForgeApiService>();
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
            
            var results = await itemService.SearchItemsAsync(context, name, exact);

            logger.LogInformation("Search returned {Count} result(s) from cache", results.Count);

            // If no results in cache, try live API search
            if (results.Count == 0)
            {
                logger.LogInformation("No cached results for '{ItemName}', attempting live API search...", name);
                await FollowupAsync($"No cached items found. Checking live API for '{name}'...");
                
                try
                {
                    logger.LogInformation("About to call FetchItemsByNameAsync for '{ItemName}'", name);
                    results = await apiService.FetchItemsByNameAsync(name);
                    logger.LogInformation("FetchItemsByNameAsync completed. Returned {Count} result(s)", results.Count);
                    
                    if (results.Count == 0)
                    {
                        logger.LogInformation("Live API returned 0 results for '{ItemName}'", name);
                        await FollowupAsync($"No items found matching '{name}' in cache or API.");
                        return;
                    }
                    
                    logger.LogInformation("Live API returned {Count} result(s) for '{ItemName}'", results.Count, name);
                    await FollowupAsync($"Found {results.Count} live API result(s). Displaying now...");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching items from live API for '{ItemName}'", name);
                    await FollowupAsync($"No cached items found and live API search failed: {ex.Message}");
                    return;
                }
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
        
        var logger = Services.GetRequiredService<ILogger<AshesModule>>();
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
        
        var logger = Services.GetRequiredService<ILogger<AshesModule>>();
        
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
    
    #endregion
    
    #region Recipe Command
    
    [SlashCommand("recipe", "Search for a crafting recipe in Ashes of Creation")]
    public async Task RecipeCommand(
        [Summary("name", "The name of the recipe or output item to search for")] string name,
        [Summary("exact", "Whether to search for exact matches only")] bool exact = false)
    {
        await DeferAsync();
        
        var logger = Services.GetRequiredService<ILogger<AshesModule>>();
        logger.LogInformation("User {UserId} searching for recipe: '{RecipeName}' (Exact: {Exact})", 
            Context.User.Id, name, exact);
        
        try
        {
            var recipeService = Services.GetRequiredService<AshesRecipeService>();
            var apiService = Services.GetRequiredService<AshesForgeApiService>();
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
            
            var results = await recipeService.SearchRecipesAsync(context, name, exact);

            logger.LogInformation("Search returned {Count} result(s) from cache", results.Count);

            // If no results in cache, try live API search
            if (results.Count == 0)
            {
                logger.LogInformation("No cached results for '{RecipeName}', attempting live API search...", name);
                await FollowupAsync($"No cached recipes found. Checking live API for '{name}'...");
                
                try
                {
                    logger.LogInformation("About to call FetchAllRecipesAsync");
                    var allRecipes = await apiService.FetchAllRecipesAsync();
                    
                    // Filter locally by name
                    results = await Task.Run(() => allRecipes
                        .Where(r => r.Name.Contains(name, StringComparison.OrdinalIgnoreCase) || 
                                   r.OutputItemName.Contains(name, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(r => r.Name)
                        .ToList());
                    
                    logger.LogInformation("Live API search returned {Count} result(s)", results.Count);
                    
                    if (results.Count == 0)
                    {
                        logger.LogInformation("Live API returned 0 results for '{RecipeName}'", name);
                        await FollowupAsync($"No recipes found matching '{name}' in cache or API.");
                        return;
                    }
                    
                    logger.LogInformation("Live API returned {Count} result(s) for '{RecipeName}'", results.Count, name);
                    await FollowupAsync($"Found {results.Count} live API result(s). Displaying now...");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching recipes from live API for '{RecipeName}'", name);
                    await FollowupAsync($"No cached recipes found and live API search failed: {ex.Message}");
                    return;
                }
            }

            if (results.Count == 1)
            {
                // Load the full recipe with ingredients (will enrich on-the-fly if needed)
                var fullRecipe = await recipeService.GetRecipeByIdAsync(context, results[0].RecipeId);
                if (fullRecipe != null)
                {
                    var recipeEmbed = await recipeService.BuildRecipeEmbedAsync(context, fullRecipe);
                    await FollowupAsync(embed: recipeEmbed);
                }
                else
                {
                    await FollowupAsync("Recipe not found.");
                }
                
                // Add button for raw materials view
                var buttons = new ComponentBuilder()
                    .WithButton("View Raw Materials", $"raw_materials:{results[0].RecipeId}:{Context.User.Id}", ButtonStyle.Secondary)
                    .Build();
                
                await FollowupAsync(components: buttons);
                return;
            }

            // Multiple results - show selection menu
            var selectMenu = new SelectMenuBuilder()
                .WithCustomId($"recipe_select:{Context.User.Id}")
                .WithPlaceholder($"Select a recipe ({results.Count} results)")
                .WithMinValues(1)
                .WithMaxValues(1);

            var displayCount = Math.Min(results.Count, 25);
            for (int i = 0; i < displayCount; i++)
            {
                var recipe = results[i];
                var label = recipe.Name.Length > 100 ? recipe.Name[..97] + "..." : recipe.Name;
                var description = $"{recipe.Profession} Lvl {recipe.ProfessionLevel} → {recipe.OutputItemName}";
                if (description.Length > 100)
                    description = description[..97] + "...";
                
                selectMenu.AddOption(label: label, value: recipe.RecipeId, description: description);
            }

            var component = new ComponentBuilder().WithSelectMenu(selectMenu).Build();

            var selectionEmbed = new EmbedBuilder()
                .WithTitle($"Found {results.Count} recipes matching '{name}'")
                .WithDescription(displayCount < results.Count 
                    ? $"Showing first {displayCount} results. Please refine your search."
                    : "Please select a recipe from the dropdown below:")
                .WithColor(Color.Gold)
                .Build();

            await FollowupAsync(embed: selectionEmbed, components: component);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing recipe search for '{RecipeName}'", name);
            await FollowupAsync($"An error occurred while searching for '{name}': {ex.Message}");
        }
    }

    [ComponentInteraction("recipe_select:*", true)]
    public async Task HandleRecipeSelection(string userId, string[] selectedValues)
    {
        await DeferAsync();
        
        var logger = Services.GetRequiredService<ILogger<AshesModule>>();
        logger.LogWarning("HandleRecipeSelection - UserId: {UserId}, Values: {Values}", 
            userId, string.Join(", ", selectedValues));
        
        try
        {
            if (Context.User.Id.ToString() != userId)
            {
                await FollowupAsync("This selection menu is not for you.", ephemeral: true);
                return;
            }

            var recipeService = Services.GetRequiredService<AshesRecipeService>();
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PandaBotContext>();

            var recipeId = selectedValues[0];
            logger.LogInformation("Looking up recipe with ID: {RecipeId}", recipeId);
            
            var recipe = await recipeService.GetRecipeByIdAsync(context, recipeId);

            if (recipe == null)
            {
                await FollowupAsync("Recipe not found.");
                return;
            }

            var embed = await recipeService.BuildRecipeEmbedAsync(context, recipe);
            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling recipe selection");
            await FollowupAsync($"An error occurred: {ex.Message}");
        }
    }

    [ComponentInteraction("raw_materials:*:*", true)]
    public async Task HandleRawMaterialsButton(string recipeId, string userId)
    {
        await DeferAsync();
        
        var logger = Services.GetRequiredService<ILogger<AshesModule>>();
        logger.LogWarning("HandleRawMaterialsButton - UserId: {UserId}, RecipeId: {RecipeId}", userId, recipeId);
        
        try
        {
            if (Context.User.Id.ToString() != userId)
            {
                await FollowupAsync("This button is not for you.", ephemeral: true);
                return;
            }

            var recipeService = Services.GetRequiredService<AshesRecipeService>();
            var apiService = Services.GetRequiredService<AshesForgeApiService>();
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PandaBotContext>();

            var recipe = await recipeService.GetRecipeByIdAsync(context, recipeId);

            if (recipe == null)
            {
                await FollowupAsync("Recipe not found.");
                return;
            }

            var embed = await recipeService.BuildRecipeWithRawMaterialsEmbedAsync(context, recipe, apiService);
            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling raw materials button");
            await FollowupAsync($"An error occurred: {ex.Message}", ephemeral: true);
        }
    }
    
    #endregion
}
