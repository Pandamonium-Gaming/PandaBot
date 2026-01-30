using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Models.AshesOfCreation;
using System.Text;

namespace PandaBot.Services.AshesOfCreation;

public class AshesItemService
{
    private readonly IMemoryCache _cache;
    private readonly ImageCacheService _imageCache;
    private readonly ILogger<AshesItemService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AshesItemService(
        IMemoryCache cache,
        ImageCacheService imageCache,
        ILogger<AshesItemService> logger,
        IServiceProvider serviceProvider)
    {
        _cache = cache;
        _imageCache = imageCache;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<List<CachedItem>> SearchItemsAsync(PandaBotContext context, string query, bool exactMatch = false)
    {
        query = query.Trim();
        _logger.LogInformation("Searching for items: '{Query}' (Exact: {ExactMatch})", query, exactMatch);

        List<CachedItem> results;

        try
        {
            _logger.LogDebug("Querying database...");
            
            if (exactMatch)
            {
                results = await context.CachedItems
                    .Where(i => i.Name.ToLower() == query.ToLower())
                    .OrderBy(i => i.Name)
                    .AsNoTracking()
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} exact match(es) in cache", results.Count);
            }
            else
            {
                // Fuzzy search - contains match with relevance scoring
                results = await context.CachedItems
                    .Where(i => EF.Functions.Like(i.Name, $"%{query}%"))
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Found {Count} fuzzy match(es) in cache", results.Count);

                // Sort by relevance (starts with query > contains query)
                results = results
                    .OrderBy(i => i.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(i => i.Name.Length)
                    .ThenBy(i => i.Name)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying database");
            results = new List<CachedItem>();
        }

        _logger.LogInformation("Returning {Count} total results", results.Count);

        // If no results found in cache, trigger background cache refresh
        if (results.Count == 0)
        {
            _logger.LogWarning("No cached results for '{Query}'. Database might be empty. Wait for background service to populate cache.", query);
        }

        return results;
    }

    public async Task<CachedItem?> GetItemByIdAsync(PandaBotContext context, string itemId)
    {
        return await context.CachedItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ItemId == itemId);
    }

    public async Task<Embed> BuildItemEmbedAsync(PandaBotContext context, CachedItem item)
    {
        var embed = new EmbedBuilder()
            .WithTitle(item.Name)
            .WithColor(GetRarityColor(item.Rarity))
            .WithUrl($"https://www.ashesforge.com/items/{item.ItemId}");

        if (!string.IsNullOrEmpty(item.Description))
            embed.WithDescription(item.Description);

        // Basic info
        if (!string.IsNullOrWhiteSpace(item.Rarity))
            embed.AddField("Rarity", item.Rarity, inline: true);

        if (item.Level.HasValue)
            embed.AddField("Level", item.Level.Value.ToString(), inline: true);

        if (!string.IsNullOrWhiteSpace(item.Type))
            embed.AddField("Type", item.Type, inline: true);

        if (!string.IsNullOrWhiteSpace(item.Category))
            embed.AddField("Category", item.Category, inline: true);

        if (!string.IsNullOrWhiteSpace(item.VendorValueType))
            embed.AddField("Vendor Type", item.VendorValueType, inline: true);

        // Properties
        if (item.IsStackable)
            embed.AddField("Stackable", item.MaxStackSize.HasValue ? $"Yes (Max: {item.MaxStackSize})" : "Yes", inline: true);

        if (item.Enchantable)
            embed.AddField("Enchantable", "Yes", inline: true);

        if (!string.IsNullOrWhiteSpace(item.SlotType))
            embed.AddField("Slot", item.SlotType, inline: true);

        // Recipe usage - match by ItemId only since ItemName is empty in ingredients
        var recipes = await context.CachedRecipeIngredients
            .Where(ri => ri.ItemId == item.ItemId)
            .Include(ri => ri.CachedCraftingRecipe)
            .Select(ri => ri.CachedCraftingRecipe)
            .Distinct()
            .ToListAsync();

        if (recipes.Any())
        {
            var recipeCount = recipes.Count;
            var topRecipes = recipes
                .OrderByDescending(r => r.Views)
                .Take(5)
                .ToList();

            var recipeText = new StringBuilder();
            recipeText.AppendLine($"Used in **{recipeCount}** recipe{(recipeCount != 1 ? "s" : "")}");
            
            if (topRecipes.Any())
            {
                recipeText.AppendLine("\n**Most Popular:**");
                foreach (var recipe in topRecipes)
                {
                    var viewCount = recipe.Views > 0 ? $" ({recipe.Views:N0} views)" : "";
                    recipeText.AppendLine($"• {recipe.Name}{viewCount}");
                }
            }

            embed.AddField("📜 Crafting Recipes", recipeText.ToString(), inline: false);

            // Calculate highest required skill level across all recipes using this item
            var recipesByProfession = recipes
                .GroupBy(r => r.Profession)
                .Select(g => new
                {
                    Profession = g.Key,
                    MaxLevel = g.Max(r => r.ProfessionLevel)
                })
                .OrderByDescending(x => x.MaxLevel)
                .ToList();

            if (recipesByProfession.Any())
            {
                var skillText = new StringBuilder();
                foreach (var prof in recipesByProfession)
                {
                    var levelName = GetLevelNameFromNumber(prof.MaxLevel);
                    skillText.AppendLine($"• {prof.Profession} - {levelName}");
                }
                embed.AddField("⚙️ Highest Required Skill", skillText.ToString().TrimEnd(), inline: false);
            }
        }

        // Stats
        if (item.Views > 0)
            embed.AddField("📊 Wiki Views", item.Views.ToString("N0"), inline: true);

        // Use cached image or full URL
        if (!string.IsNullOrEmpty(item.LocalImagePath) && File.Exists(item.LocalImagePath))
            embed.WithImageUrl($"attachment://{Path.GetFileName(item.LocalImagePath)}");
        else if (!string.IsNullOrEmpty(item.ImageUrl))
            embed.WithImageUrl(_imageCache.GetImageUrl(item.ImageUrl));

        if (!string.IsNullOrEmpty(item.IconUrl))
            embed.WithThumbnailUrl(_imageCache.GetImageUrl(item.IconUrl));

        embed.WithFooter($"Item ID: {item.ItemId} | Data from AshesForge")
            .WithTimestamp(item.LastUpdated);

        return embed.Build();
    }

    // Keep the synchronous version for backward compatibility but mark it obsolete
    [Obsolete("Use BuildItemEmbedAsync instead")]
    public Embed BuildItemEmbed(CachedItem item)
    {
        var embed = new EmbedBuilder()
            .WithTitle(item.Name)
            .WithColor(GetRarityColor(item.Rarity))
            .WithUrl($"https://www.ashesforge.com/items/{item.ItemId}");

        if (!string.IsNullOrEmpty(item.Description))
            embed.WithDescription(item.Description);

        // Basic info
        if (!string.IsNullOrWhiteSpace(item.Rarity))
            embed.AddField("Rarity", item.Rarity, inline: true);

        if (item.Level.HasValue)
            embed.AddField("Level", item.Level.Value.ToString(), inline: true);

        if (!string.IsNullOrWhiteSpace(item.Type))
            embed.AddField("Type", item.Type, inline: true);

        if (!string.IsNullOrWhiteSpace(item.Category))
            embed.AddField("Category", item.Category, inline: true);

        if (!string.IsNullOrWhiteSpace(item.VendorValueType))
            embed.AddField("Vendor Type", item.VendorValueType, inline: true);

        // Properties
        if (item.IsStackable)
            embed.AddField("Stackable", item.MaxStackSize.HasValue ? $"Yes (Max: {item.MaxStackSize})" : "Yes", inline: true);

        if (item.Enchantable)
            embed.AddField("Enchantable", "Yes", inline: true);

        if (!string.IsNullOrWhiteSpace(item.SlotType))
            embed.AddField("Slot", item.SlotType, inline: true);

        // Stats
        if (item.Views > 0)
            embed.AddField("📊 Wiki Views", item.Views.ToString("N0"), inline: true);

        // Use cached image or full URL
        if (!string.IsNullOrEmpty(item.LocalImagePath) && File.Exists(item.LocalImagePath))
            embed.WithImageUrl($"attachment://{Path.GetFileName(item.LocalImagePath)}");
        else if (!string.IsNullOrEmpty(item.ImageUrl))
            embed.WithImageUrl(_imageCache.GetImageUrl(item.ImageUrl));

        if (!string.IsNullOrEmpty(item.IconUrl))
            embed.WithThumbnailUrl(_imageCache.GetImageUrl(item.IconUrl));

        embed.WithFooter($"Item ID: {item.ItemId} | Data from AshesForge")
            .WithTimestamp(item.LastUpdated);

        return embed.Build();
    }

    private Color GetRarityColor(string? rarity)
    {
        return rarity?.ToLowerInvariant() switch
        {
            "common" => Color.LightGrey,
            "uncommon" => Color.Green,
            "rare" => Color.Blue,
            "epic" => new Color(163, 53, 238), // Purple
            "legendary" => Color.Orange,
            "artifact" => Color.Red,
            _ => Color.Default
        };
    }

    private string GetLevelNameFromNumber(int levelNumber)
    {
        return levelNumber switch
        {
            0 => "Novice",
            1 => "Apprentice",
            2 => "Journeyman",
            3 => "Master",
            4 => "Legendary",
            5 => "Ancient",
            _ => levelNumber.ToString()
        };
    }
}
