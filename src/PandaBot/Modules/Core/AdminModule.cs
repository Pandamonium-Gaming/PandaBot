using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;

namespace PandaBot.Modules.Core;

[Group("admin", "Admin commands")]
[DefaultMemberPermissions(GuildPermission.Administrator)]
public class AdminModule : InteractionModuleBase<SocketInteractionContext>
{
    public IServiceProvider Services { get; set; } = null!;

    [SlashCommand("sync-commands", "Sync slash commands to Discord")]
    public async Task SyncCommandsAsync()
    {
        await DeferAsync();
        
        var logger = Services.GetRequiredService<ILogger<AdminModule>>();
        var interactionService = Services.GetRequiredService<InteractionService>();

        try
        {
            logger.LogInformation("Syncing commands to guild {GuildId}...", Context.Guild.Id);
            
            var commands = await interactionService.RegisterCommandsToGuildAsync(Context.Guild.Id);
            
            logger.LogInformation("Synced {Count} commands", commands.Count);
            
            await FollowupAsync($"✅ Synced {commands.Count} slash commands to this guild!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing commands");
            await FollowupAsync($"❌ Error syncing commands: {ex.Message}");
        }
    }

    [SlashCommand("sync-global", "Sync slash commands globally (takes up to 1 hour)")]
    public async Task SyncGlobalAsync()
    {
        await DeferAsync();
        
        var logger = Services.GetRequiredService<ILogger<AdminModule>>();
        var interactionService = Services.GetRequiredService<InteractionService>();

        try
        {
            logger.LogInformation("Syncing commands globally...");
            
            var commands = await interactionService.RegisterCommandsGloballyAsync();
            
            logger.LogInformation("Synced {Count} commands globally", commands.Count);
            
            await FollowupAsync($"✅ Synced {commands.Count} slash commands globally! This may take up to 1 hour to appear.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing global commands");
            await FollowupAsync($"❌ Error syncing commands: {ex.Message}");
        }
    }

    [SlashCommand("purge-cache", "Purge specific database caches")]
    public async Task PurgeCacheAsync(
        [Summary("target", "What to purge: items, recipes, ingredients, or all")] string target)
    {
        await DeferAsync();
        
        var logger = Services.GetRequiredService<ILogger<AdminModule>>();
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PandaBotContext>();

        try
        {
            var target_lower = target.ToLower();
            int deletedCount = 0;

            switch (target_lower)
            {
                case "items":
                    deletedCount = await context.CachedItems.ExecuteDeleteAsync();
                    logger.LogWarning("Purged {Count} cached items", deletedCount);
                    await FollowupAsync($"✅ Purged {deletedCount:N0} cached items from the database.");
                    break;

                case "recipes":
                    // Delete recipes (this will cascade to ingredients due to foreign keys)
                    deletedCount = await context.CachedCraftingRecipes.ExecuteDeleteAsync();
                    logger.LogWarning("Purged {Count} cached recipes", deletedCount);
                    await FollowupAsync($"✅ Purged {deletedCount:N0} cached recipes and their ingredients from the database.");
                    break;

                case "ingredients":
                    deletedCount = await context.CachedRecipeIngredients.ExecuteDeleteAsync();
                    logger.LogWarning("Purged {Count} recipe ingredients", deletedCount);
                    await FollowupAsync($"✅ Purged {deletedCount:N0} recipe ingredients from the database. Run `/admin purge-cache recipes` to re-fetch recipes with ingredients.");
                    break;

                case "all":
                    var itemCount = await context.CachedItems.CountAsync();
                    var recipeCount = await context.CachedCraftingRecipes.CountAsync();
                    var ingredientCount = await context.CachedRecipeIngredients.CountAsync();
                    
                    await context.CachedItems.ExecuteDeleteAsync();
                    await context.CachedCraftingRecipes.ExecuteDeleteAsync();
                    await context.CachedRecipeIngredients.ExecuteDeleteAsync();
                    
                    logger.LogWarning("Purged all caches - Items: {ItemCount}, Recipes: {RecipeCount}, Ingredients: {IngredientCount}", 
                        itemCount, recipeCount, ingredientCount);
                    await FollowupAsync($"✅ Purged all caches:\n• Items: {itemCount:N0}\n• Recipes: {recipeCount:N0}\n• Ingredients: {ingredientCount:N0}\n\nThe background service will re-fetch data from the API.");
                    break;

                default:
                    await FollowupAsync($"❌ Unknown target '{target}'. Use: `items`, `recipes`, `ingredients`, or `all`");
                    return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error purging cache for target {Target}", target);
            await FollowupAsync($"❌ Error purging cache: {ex.Message}");
        }
    }
}
