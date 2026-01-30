using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Models.AshesOfCreation;
using System.Text;

namespace PandaBot.Services.AshesOfCreation;

public class AshesRecipeService
{
    private readonly IMemoryCache _cache;
    private readonly ImageCacheService _imageCache;
    private readonly ILogger<AshesRecipeService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AshesRecipeService(
        IMemoryCache cache,
        ImageCacheService imageCache,
        ILogger<AshesRecipeService> logger,
        IServiceProvider serviceProvider)
    {
        _cache = cache;
        _imageCache = imageCache;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<List<CachedCraftingRecipe>> SearchRecipesAsync(PandaBotContext context, string query, bool exactMatch = false)
    {
        query = query.Trim();
        _logger.LogInformation("Searching for recipes: '{Query}' (Exact: {ExactMatch})", query, exactMatch);

        List<CachedCraftingRecipe> results;

        try
        {
            _logger.LogDebug("Querying database...");
            
            if (exactMatch)
            {
                results = await context.CachedCraftingRecipes
                    .Where(r => r.Name.ToLower() == query.ToLower() || r.OutputItemName.ToLower() == query.ToLower())
                    .Include(r => r.Ingredients)
                    .OrderBy(r => r.Name)
                    .AsNoTracking()
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} exact match(es) in cache", results.Count);
            }
            else
            {
                // Fuzzy search - contains match with relevance scoring
                results = await context.CachedCraftingRecipes
                    .Where(r => EF.Functions.Like(r.Name, $"%{query}%") || EF.Functions.Like(r.OutputItemName, $"%{query}%"))
                    .Include(r => r.Ingredients)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Found {Count} fuzzy match(es) in cache", results.Count);

                // Sort by relevance (starts with query > contains query, then by name)
                results = results
                    .OrderBy(r => r.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(r => r.Name.Length)
                    .ThenBy(r => r.Name)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying database");
            results = new List<CachedCraftingRecipe>();
        }

        _logger.LogInformation("Returning {Count} total results", results.Count);

        // If no results found in cache, log warning
        if (results.Count == 0)
        {
            _logger.LogWarning("No cached results for '{Query}'. Database might be empty. Wait for background service to populate cache.", query);
        }

        return results;
    }

    public async Task<CachedCraftingRecipe?> GetRecipeByIdAsync(PandaBotContext context, string recipeId)
    {
        _logger.LogWarning("GetRecipeByIdAsync called for recipe: {RecipeId}", recipeId);
        
        var recipe = await context.CachedCraftingRecipes
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId);
        
        _logger.LogWarning("Recipe loaded: {RecipeId}, has {IngredientCount} ingredients", 
            recipeId, recipe?.Ingredients.Count ?? 0);
        
        // If recipe has no ingredients, try to enrich it on-the-fly
        if (recipe != null && !recipe.Ingredients.Any())
        {
            _logger.LogWarning("No ingredients found for {RecipeId}, attempting on-the-fly enrichment...", recipeId);
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var apiService = scope.ServiceProvider.GetRequiredService<AshesForgeApiService>();
                await apiService.EnrichSingleRecipeAsync(recipe);
                
                _logger.LogWarning("On-the-fly enrichment completed for {RecipeId}", recipeId);
                
                // Reload the recipe to get updated ingredients
                var enrichedRecipe = await context.CachedCraftingRecipes
                    .Include(r => r.Ingredients)
                    .FirstOrDefaultAsync(r => r.RecipeId == recipeId);
                
                _logger.LogWarning("Reloaded recipe after enrichment: {RecipeId} now has {IngredientCount} ingredients", 
                    recipeId, enrichedRecipe?.Ingredients.Count ?? 0);
                
                return enrichedRecipe;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich recipe {RecipeId} on-the-fly", recipeId);
                // Fall through and return recipe without ingredients
            }
        }
        
        return recipe;
    }

    public async Task<Embed> BuildRecipeEmbedAsync(PandaBotContext context, CachedCraftingRecipe recipe)
    {
        _logger.LogInformation("Building recipe embed for: {RecipeName}, Ingredients count: {IngredientCount}", 
            recipe.Name, recipe.Ingredients?.Count ?? 0);
        
        // Load output item to get rarity information and attributes
        var outputItem = recipe.OutputItem ?? 
            await context.CachedItems.FirstOrDefaultAsync(i => i.ItemId == recipe.OutputItemId);

        var embed = new EmbedBuilder()
            .WithUrl($"https://www.ashesforge.com/recipes/{recipe.RecipeId}");

        // Build title with output item name and quality range
        var title = recipe.OutputItemName;
        title += " [Common - Legendary]"; // Show possible quality range
        embed.WithTitle(title);

        // Set embed colour based on rarity
        var colour = GetColourForRarity(outputItem?.Rarity);
        embed.WithColor(colour);

        // Add item icon as thumbnail if available
        if (!string.IsNullOrEmpty(outputItem?.IconUrl))
        {
            var iconUrl = _imageCache.GetImageUrl(outputItem.IconUrl);
            embed.WithThumbnailUrl(iconUrl);
        }

        // Convert profession level to human-readable format
        var professionLevel = GetProfessionLevelName(recipe.Profession, recipe.ProfessionLevel);
        var professionInfo = $"{recipe.Profession} - {professionLevel}";
        embed.AddField("Crafter Level", professionInfo, inline: true);

        // Add station if available
        if (!string.IsNullOrEmpty(recipe.Station))
        {
            embed.AddField("Station", recipe.Station, inline: true);
        }

        // Add craft time if available
        if (recipe.CraftTime > 0)
        {
            var craftTimeStr = recipe.CraftTime >= 60 
                ? $"{recipe.CraftTime / 60}m {recipe.CraftTime % 60}s"
                : $"{recipe.CraftTime}s";
            embed.AddField("Craft Time", craftTimeStr, inline: true);
        }

        // Add output item with quantity
        var outputText = $"{recipe.OutputItemName}";
        if (recipe.OutputQuantity > 1)
            outputText += $" x{recipe.OutputQuantity}";
        embed.AddField("Produces", outputText, inline: false);

        // Add ingredients if available - make sure they're loaded
        if (recipe.Ingredients?.Count > 0)
        {
            _logger.LogInformation("Recipe has {Count} ingredients", recipe.Ingredients.Count);
            var ingredientList = new StringBuilder();
            foreach (var ingredient in recipe.Ingredients.OrderBy(i => i.ItemName))
            {
                _logger.LogInformation("  Ingredient: {ItemName} x{Quantity}", ingredient.ItemName, ingredient.Quantity);
                ingredientList.AppendLine($"• {ingredient.ItemName} x{ingredient.Quantity}");
            }
            var ingredientText = ingredientList.ToString().TrimEnd();
            if (!string.IsNullOrEmpty(ingredientText))
            {
                embed.AddField("Ingredients Needed", ingredientText, inline: false);
            }
        }
        else
        {
            _logger.LogWarning("Recipe {RecipeName} has no ingredients loaded", recipe.Name);
        }

        // Add attributes if available (from RawJson)
        if (!string.IsNullOrEmpty(outputItem?.RawJson))
        {
            try
            {
                var attributes = ExtractAttributesFromJson(outputItem.RawJson);
                if (!string.IsNullOrEmpty(attributes))
                {
                    embed.AddField("Attributes", attributes, inline: false);
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        // Add view count
        if (recipe.Views > 0)
        {
            embed.AddField("Views", recipe.Views.ToString("N0"), inline: true);
        }

        embed.WithFooter($"Cached: {recipe.CachedAt:g}");

        return embed.Build();
    }

    private string ExtractAttributesFromJson(string rawJson)
    {
        try
        {
            using (var doc = System.Text.Json.JsonDocument.Parse(rawJson))
            {
                var root = doc.RootElement;
                var attributesList = new StringBuilder();

                // Look for stats array (common in Ashes of Creation API)
                if (root.TryGetProperty("stats", out var statsArray) && statsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var stat in statsArray.EnumerateArray())
                    {
                        // Get stat name
                        var statName = "Unknown";
                        if (stat.TryGetProperty("stat", out var statObj) && 
                            statObj.TryGetProperty("name", out var nameElem))
                        {
                            statName = nameElem.GetString() ?? "Unknown";
                        }

                        // Get the min/max for Common and Legendary
                        var commonMin = GetStatValue(stat, "commonMin");
                        var commonMax = GetStatValue(stat, "commonMax");
                        var legendaryMin = GetStatValue(stat, "legendaryMin");
                        var legendaryMax = GetStatValue(stat, "legendaryMax");

                        // Only show if we have actual values
                        if (commonMin > 0 || commonMax > 0 || legendaryMin > 0 || legendaryMax > 0)
                        {
                            if (commonMin == commonMax && legendaryMin == legendaryMax)
                            {
                                // Fixed value
                                attributesList.AppendLine($"• {statName}: {commonMin}");
                            }
                            else if (commonMin == commonMax && legendaryMin > legendaryMax)
                            {
                                // Only show common range
                                attributesList.AppendLine($"• {statName}: {commonMin}");
                            }
                            else if (commonMin > 0 && legendaryMax > 0)
                            {
                                // Show range from Common to Legendary
                                attributesList.AppendLine($"• {statName}: {commonMin}-{legendaryMax}");
                            }
                            else if (commonMin > 0)
                            {
                                attributesList.AppendLine($"• {statName}: {commonMin}-{commonMax}");
                            }
                        }
                    }
                }

                return attributesList.ToString().TrimEnd();
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    private int GetStatValue(System.Text.Json.JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == System.Text.Json.JsonValueKind.Number)
                return prop.GetInt32();
        }
        return 0;
    }

    private Color GetColourForRarity(string? rarity)
    {
        return rarity?.ToLower() switch
        {
            "common" => Color.LighterGrey,
            "uncommon" => Color.Green,
            "rare" => Color.Blue,
            "epic" => Color.Purple,
            "legendary" => Color.Gold,
            "mythic" => Color.Orange,
            _ => Color.Gold // Default colour
        };
    }

    private string GetProfessionLevelName(string profession, int level)
    {
        return level switch
        {
            1 => "Apprentice I",
            2 => "Apprentice II",
            3 => "Apprentice III",
            4 => "Journeyman I",
            5 => "Journeyman II",
            6 => "Journeyman III",
            7 => "Artisan I",
            8 => "Artisan II",
            9 => "Artisan III",
            10 => "Master I",
            11 => "Master II",
            12 => "Master III",
            13 => "Expert I",
            14 => "Expert II",
            15 => "Expert III",
            16 => "Grandmaster I",
            17 => "Grandmaster II",
            18 => "Grandmaster III",
            _ => $"Level {level}"
        };
    }

    public async Task<Embed> BuildRecipeWithRawMaterialsEmbedAsync(PandaBotContext context, CachedCraftingRecipe recipe, AshesForgeApiService apiService)
    {
        _logger.LogInformation("Building raw materials embed for: {RecipeName}", recipe.Name);
        
        // Log the ingredients we have
        var ingredients = recipe.Ingredients?.ToList() ?? new List<CachedRecipeIngredient>();
        foreach (var ing in ingredients)
        {
            _logger.LogWarning("Ingredient: {Name} (ID: {ItemId}) x{Qty}", ing.ItemName, ing.ItemId ?? "[NULL]", ing.Quantity);
        }
        
        var baseEmbed = await BuildRecipeEmbedAsync(context, recipe);
        
        // Fetch all raw materials recursively
        var rawMaterials = new Dictionary<string, int>(); // ItemName -> Total Quantity
        await FetchRawMaterialsRecursiveAsync(context, ingredients, rawMaterials, apiService);
        
        // Log final raw materials
        _logger.LogWarning("=== Final Raw Materials ({Count} items) ===", rawMaterials.Count);
        foreach (var rm in rawMaterials)
        {
            _logger.LogWarning("  {ItemName}: {Qty}", rm.Key, rm.Value);
        }
        
        // Create new embed with raw materials
        var embed = new EmbedBuilder()
            .WithTitle($"{baseEmbed.Title} [Raw Materials Breakdown]")
            .WithColor(baseEmbed.Color ?? Color.Gold)
            .WithUrl(baseEmbed.Url)
            .WithThumbnailUrl(baseEmbed.Thumbnail?.Url);

        // Copy craft level info
        foreach (var field in baseEmbed.Fields.Where(f => f.Name == "Crafter Level" || f.Name == "Station" || f.Name == "Craft Time"))
        {
            embed.AddField(field.Name, field.Value, field.Inline);
        }

        // Add produces
        var producesField = baseEmbed.Fields.FirstOrDefault(f => f.Name == "Produces");
        var producesValue = producesField != null ? producesField.Value : recipe.OutputItemName;
        embed.AddField("Produces", producesValue, inline: false);

        // Add direct ingredients
        var directIngredients = new StringBuilder();
        foreach (var ingredient in (recipe.Ingredients ?? new List<CachedRecipeIngredient>()).OrderBy(i => i.ItemName))
        {
            directIngredients.AppendLine($"• {ingredient.ItemName} x{ingredient.Quantity}");
        }
        if (directIngredients.Length > 0)
        {
            embed.AddField("Direct Ingredients", directIngredients.ToString().TrimEnd(), inline: false);
        }

        // Add raw materials breakdown
        if (rawMaterials.Count > 0)
        {
            var rawMaterialsList = new StringBuilder();
            foreach (var material in rawMaterials.OrderBy(x => x.Key))
            {
                rawMaterialsList.AppendLine($"• {material.Key} x{material.Value}");
            }
            embed.AddField("Raw Materials", rawMaterialsList.ToString().TrimEnd(), inline: false);
        }

        embed.WithFooter($"Cached: {recipe.CachedAt:g}");
        return embed.Build();
    }

    private async Task FetchRawMaterialsRecursiveAsync(
        PandaBotContext context, 
        List<CachedRecipeIngredient>? ingredients, 
        Dictionary<string, int> rawMaterials,
        AshesForgeApiService apiService,
        int depth = 0,
        int maxDepth = 5)
    {
        if (depth >= maxDepth || ingredients == null || ingredients.Count == 0)
            return;

        _logger.LogWarning("=== FetchRawMaterials Depth {Depth}: Processing {Count} ingredients ===", depth, ingredients.Count);

        foreach (var ingredient in ingredients)
        {
            try
            {
                _logger.LogWarning("Processing ingredient: {ItemName} (ID: {ItemId}) x{Quantity}", 
                    ingredient.ItemName, string.IsNullOrEmpty(ingredient.ItemId) ? "[EMPTY]" : ingredient.ItemId, ingredient.Quantity);

                // Try to find item in cache first
                var itemInCache = await context.CachedItems.FirstOrDefaultAsync(i => i.ItemId == ingredient.ItemId);
                
                if (itemInCache != null)
                {
                    _logger.LogWarning("  ✓ Found {ItemName} in cache", ingredient.ItemName);
                }
                
                System.Text.Json.JsonElement? itemDetails = null;
                if (itemInCache?.RawJson != null)
                {
                    _logger.LogWarning("  Using cached RawJson for {ItemName}", ingredient.ItemName);
                    itemDetails = System.Text.Json.JsonDocument.Parse(itemInCache.RawJson).RootElement;
                }
                else if (!string.IsNullOrEmpty(ingredient.ItemId))
                {
                    _logger.LogWarning("  Fetching {ItemName} from API (ItemId: {ItemId})", ingredient.ItemName, ingredient.ItemId);
                    itemDetails = await apiService.FetchItemDetailsAsync(ingredient.ItemId);
                    if (itemDetails != null)
                    {
                        _logger.LogWarning("  ✓ API fetch succeeded for {ItemName}", ingredient.ItemName);
                    }
                    else
                    {
                        _logger.LogWarning("  ✗ API fetch FAILED for {ItemName}", ingredient.ItemName);
                    }
                }
                else
                {
                    _logger.LogWarning("  ✗ No ItemId for {ItemName}, cannot fetch from API", ingredient.ItemName);
                }

                if (!itemDetails.HasValue || itemDetails.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                {
                    _logger.LogWarning("  → Treating {ItemName} as raw material (no item details)", ingredient.ItemName);
                    // Treat as raw material if can't fetch
                    AddRawMaterial(rawMaterials, ingredient.ItemName, ingredient.Quantity);
                    continue;
                }

                _logger.LogWarning("  ✓ Fetched item details for {ItemName}", ingredient.ItemName);

                // Check if this item has a recipe (can be crafted)
                if (itemDetails.HasValue && itemDetails.Value.TryGetProperty("createdByRecipes", out var recipesProperty) && 
                    recipesProperty.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var recipesArray = recipesProperty.EnumerateArray().ToList();
                    _logger.LogWarning("  ✓ {ItemName} has {RecipeCount} recipe(s) in createdByRecipes array", ingredient.ItemName, recipesArray.Count);
                    
                    if (recipesArray.Count > 0)
                    {
                        var recipeData = recipesArray[0];
                        _logger.LogWarning("    Using first recipe from createdByRecipes array");
                        
                        // This item has a recipe, fetch its ingredients recursively
                        if (recipeData.TryGetProperty("inputs", out var inputsProperty) && 
                            inputsProperty.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            _logger.LogWarning("    Found inputs array with {Count} groups", inputsProperty.GetArrayLength());
                            var subIngredients = new List<CachedRecipeIngredient>();
                            
                            foreach (var inputGroup in inputsProperty.EnumerateArray())
                            {
                                var groupQuantity = GetJsonIntProperty(inputGroup, "quantity") ?? 1;
                                
                                if (inputGroup.TryGetProperty("items", out var itemsArray) && 
                                    itemsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var subIngredientJson in itemsArray.EnumerateArray())
                                    {
                                        var subIngredient = new CachedRecipeIngredient
                                        {
                                            ItemId = GetJsonStringProperty(subIngredientJson, "id") ?? 
                                                    GetJsonStringProperty(subIngredientJson, "itemId") ?? string.Empty,
                                            ItemName = GetJsonStringProperty(subIngredientJson, "name") ?? 
                                                      GetJsonStringProperty(subIngredientJson, "itemName") ?? string.Empty,
                                            Quantity = groupQuantity * ingredient.Quantity
                                        };
                                        
                                        if (!string.IsNullOrEmpty(subIngredient.ItemId) || !string.IsNullOrEmpty(subIngredient.ItemName))
                                        {
                                            _logger.LogWarning("  Found sub-ingredient: {ItemName} x{Quantity}", 
                                                subIngredient.ItemName, subIngredient.Quantity);
                                            subIngredients.Add(subIngredient);
                                        }
                                    }
                                }
                            }

                            if (subIngredients.Count > 0)
                            {
                                _logger.LogWarning("Recursing into {Count} sub-ingredients", subIngredients.Count);
                                // Recursively fetch raw materials for this item's ingredients
                                await FetchRawMaterialsRecursiveAsync(context, subIngredients, rawMaterials, apiService, depth + 1, maxDepth);
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("  ✗ {ItemName} has no createdByRecipes or empty array", ingredient.ItemName);
                }

                // If no recipe, treat as raw material
                AddRawMaterial(rawMaterials, ingredient.ItemName, ingredient.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing ingredient {ItemName} for raw materials", ingredient.ItemName);
                AddRawMaterial(rawMaterials, ingredient.ItemName, ingredient.Quantity);
            }
        }
    }

    private void AddRawMaterial(Dictionary<string, int> rawMaterials, string itemName, int quantity)
    {
        if (string.IsNullOrEmpty(itemName))
            return;

        if (rawMaterials.ContainsKey(itemName))
            rawMaterials[itemName] += quantity;
        else
            rawMaterials[itemName] = quantity;
    }

    private string? GetJsonStringProperty(System.Text.Json.JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private int? GetJsonIntProperty(System.Text.Json.JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.Number)
            return prop.GetInt32();
        return null;
    }
}
