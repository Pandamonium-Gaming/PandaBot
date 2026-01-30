using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Models.AshesOfCreation;
using System.Text;
using System.Text.Json;

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
                    .AsNoTracking()
                    .Where(r => r.Name.ToLower() == query.ToLower() || r.OutputItemName.ToLower() == query.ToLower())
                    .Include(r => r.Ingredients)
                    .OrderBy(r => r.Name)
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} exact match(es) in cache", results.Count);
            }
            else
            {
                // Fuzzy search - contains match with relevance scoring
                results = await context.CachedCraftingRecipes
                    .AsNoTracking()
                    .Where(r => EF.Functions.Like(r.Name, $"%{query}%") || EF.Functions.Like(r.OutputItemName, $"%{query}%"))
                    .Include(r => r.Ingredients)
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
                
                // Enrich with timeout - wait up to 5 seconds for enrichment to complete
                var enrichTask = apiService.EnrichSingleRecipeAsync(recipe);
                var completedTask = await Task.WhenAny(enrichTask, Task.Delay(5000));
                
                if (completedTask == enrichTask)
                {
                    _logger.LogWarning("Enrichment completed for {RecipeId}", recipeId);
                    // Reload the recipe to get updated ingredients and output item
                    var enrichedRecipe = await context.CachedCraftingRecipes
                        .Include(r => r.Ingredients)
                        .FirstOrDefaultAsync(r => r.RecipeId == recipeId);
                    
                    if (enrichedRecipe != null && enrichedRecipe.Ingredients.Any())
                    {
                        _logger.LogWarning("Reloaded recipe: {RecipeId} now has {Count} ingredients", recipeId, enrichedRecipe.Ingredients.Count);
                        return enrichedRecipe;
                    }
                }
                else
                {
                    _logger.LogWarning("Enrichment timeout for {RecipeId}, using incomplete data", recipeId);
                    // Start background enrichment to complete later
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await enrichTask;
                            _logger.LogWarning("Background enrichment completed for {RecipeId}", recipeId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Background enrichment failed for {RecipeId}", recipeId);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich recipe {RecipeId}", recipeId);
            }
        }
        
        return recipe;
    }

    public async Task<Embed> BuildRecipeEmbedAsync(PandaBotContext context, CachedCraftingRecipe recipe, bool includeRawMaterials = false, AshesForgeApiService? apiService = null)
    {
        _logger.LogInformation("Building recipe embed for: {RecipeName}, Ingredients count: {IngredientCount}, IncludeRawMaterials: {IncludeRawMaterials}", 
            recipe.Name, recipe.Ingredients?.Count ?? 0, includeRawMaterials);
        
        // Load output item to get rarity information and attributes
        var outputItem = recipe.OutputItem ?? 
            await context.CachedItems.FirstOrDefaultAsync(i => i.ItemId == recipe.OutputItemId);

        var embed = new EmbedBuilder()
            .WithUrl($"https://www.ashesforge.com/crafting-calculator?recipe={recipe.RecipeId}");

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
        // Try to get certificationLevel from RawJson first, fall back to ProfessionLevel
        var professionLevelNum = recipe.ProfessionLevel;
        if (!string.IsNullOrEmpty(recipe.RawJson))
        {
            try
            {
                var certLevel = ExtractCertificationLevelFromJson(recipe.RawJson);
                if (certLevel.HasValue)
                {
                    professionLevelNum = certLevel.Value;
                    // Update the database with the correct value
                    recipe.ProfessionLevel = professionLevelNum;
                }
            }
            catch
            {
                // Use the stored ProfessionLevel if extraction fails
            }
        }
        
        var professionLevel = GetProfessionLevelName(recipe.Profession, professionLevelNum);
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

        // Add raw materials if requested and we have the API service
        if (includeRawMaterials && apiService != null && recipe.Ingredients?.Any() == true)
        {
            try
            {
                var rawMaterials = new Dictionary<string, int>();
                await FetchRawMaterialsRecursiveAsync(context, recipe.Ingredients.ToList(), rawMaterials, apiService);
                
                if (rawMaterials.Count > 0)
                {
                    var rawMaterialList = new StringBuilder();
                    foreach (var rm in rawMaterials.OrderBy(r => r.Key))
                    {
                        rawMaterialList.AppendLine($"• {rm.Key} x{rm.Value}");
                    }
                    var rawMaterialText = rawMaterialList.ToString().TrimEnd();
                    if (!string.IsNullOrEmpty(rawMaterialText))
                    {
                        embed.AddField("Raw Materials Required", rawMaterialText, inline: false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating raw materials for recipe {RecipeName}", recipe.Name);
                // Continue without raw materials rather than failing the entire embed
            }
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

    private int? ExtractCertificationLevelFromJson(string rawJson)
    {
        try
        {
            using (var doc = JsonDocument.Parse(rawJson))
            {
                var root = doc.RootElement;
                
                // Try to get certificationLevel property
                if (root.TryGetProperty("certificationLevel", out var certLevelProp))
                {
                    if (certLevelProp.ValueKind == JsonValueKind.String)
                    {
                        var levelName = certLevelProp.GetString()?.ToLower() ?? "";
                        
                        // Parse tier (base level) and rank (I/II/III offset)
                        var baseLevels = new Dictionary<string, int>
                        {
                            { "novice", 0 },
                            { "apprentice", 1 },
                            { "journeyman", 4 },
                            { "artisan", 7 },
                            { "master", 10 },
                            { "expert", 13 },
                            { "grandmaster", 16 }
                        };
                        
                        var baseLevel = 0;
                        foreach (var tier in baseLevels.Keys)
                        {
                            if (levelName.StartsWith(tier))
                            {
                                baseLevel = baseLevels[tier];
                                
                                // Check for Roman numeral suffix (I, II, III)
                                if (levelName.Contains(" iii") || levelName.Contains(" 3"))
                                    return baseLevel + 2;
                                else if (levelName.Contains(" ii") || levelName.Contains(" 2"))
                                    return baseLevel + 1;
                                else if (levelName.Contains(" i") || levelName.Contains(" 1"))
                                    return baseLevel; // Base level is already the "I" variant
                                else
                                    return baseLevel; // No suffix, return base
                            }
                        }
                        
                        return null;
                    }
                    else if (certLevelProp.ValueKind == JsonValueKind.Number)
                    {
                        return certLevelProp.GetInt32();
                    }
                }
                
                return null;
            }
        }
        catch
        {
            return null;
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
            0 => "Novice",
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
        _logger.LogInformation("Building recipe embed with raw materials for: {RecipeName}", recipe.Name);
        
        var ingredients = recipe.Ingredients?.ToList() ?? new List<CachedRecipeIngredient>();
        var rawMaterials = new Dictionary<string, int>();
        var requiredProfessions = new HashSet<string>();
        
        // Add the main recipe's profession - extract from RawJson if available
        var mainLevel = recipe.ProfessionLevel;
        if (!string.IsNullOrEmpty(recipe.RawJson))
        {
            var certLevel = ExtractCertificationLevelFromJson(recipe.RawJson);
            if (certLevel.HasValue)
                mainLevel = certLevel.Value;
        }
        var mainProfessionLevel = GetProfessionLevelName(recipe.Profession, mainLevel);
        requiredProfessions.Add($"{recipe.Profession} - {mainProfessionLevel}");
        
        // Fetch raw materials and collect required professions
        await FetchRawMaterialsRecursiveAsync(context, ingredients, rawMaterials, apiService, depth: 0, maxDepth: 5, requiredProfessions: requiredProfessions);
        
        // Build the embed
        var outputItem = recipe.OutputItem ?? 
            await context.CachedItems.FirstOrDefaultAsync(i => i.ItemId == recipe.OutputItemId);
        
        var embed = new EmbedBuilder()
            .WithUrl($"https://www.ashesforge.com/recipes/{recipe.RecipeId}")
            .WithTitle($"{recipe.OutputItemName} [Raw Materials Breakdown]")
            .WithColor(GetColourForRarity(outputItem?.Rarity));

        if (!string.IsNullOrEmpty(outputItem?.IconUrl))
        {
            var iconUrl = _imageCache.GetImageUrl(outputItem.IconUrl);
            embed.WithThumbnailUrl(iconUrl);
        }

        // Show produces
        var outputText = $"{recipe.OutputItemName}";
        if (recipe.OutputQuantity > 1)
            outputText += $" x{recipe.OutputQuantity}";
        embed.AddField("Produces", outputText, inline: false);

        // Show direct ingredients
        if (recipe.Ingredients?.Count > 0)
        {
            var ingredientList = new StringBuilder();
            foreach (var ingredient in recipe.Ingredients.OrderBy(i => i.ItemName))
            {
                ingredientList.AppendLine($"• {ingredient.ItemName} x{ingredient.Quantity}");
            }
            var ingredientText = ingredientList.ToString().TrimEnd();
            if (!string.IsNullOrEmpty(ingredientText))
            {
                embed.AddField("Direct Ingredients", ingredientText, inline: false);
            }
        }

        // Show required professions
        if (requiredProfessions.Count > 0)
        {
            var professionsList = new StringBuilder();
            foreach (var profession in requiredProfessions.OrderBy(p => p))
            {
                professionsList.AppendLine($"• {profession}");
            }
            embed.AddField("Required Skills", professionsList.ToString().TrimEnd(), inline: false);
        }

        // Show raw materials
        if (rawMaterials.Count > 0)
        {
            var rawMaterialList = new StringBuilder();
            foreach (var rm in rawMaterials.OrderBy(r => r.Key))
            {
                rawMaterialList.AppendLine($"• {rm.Key} x{rm.Value}");
            }
            var rawMaterialText = rawMaterialList.ToString().TrimEnd();
            if (!string.IsNullOrEmpty(rawMaterialText))
            {
                embed.AddField("Raw Materials Required", rawMaterialText, inline: false);
            }
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
        int maxDepth = 5,
        HashSet<string>? requiredProfessions = null)
    {
        // Initialize the professions set on first call
        if (requiredProfessions == null && depth == 0)
            requiredProfessions = new HashSet<string>();
        
        if (depth >= maxDepth || ingredients == null || ingredients.Count == 0)
            return;

        _logger.LogWarning("=== FetchRawMaterials Depth {Depth}: Processing {Count} ingredients ===", depth, ingredients.Count);

        foreach (var ingredient in ingredients)
        {
            try
            {
                _logger.LogWarning("Processing ingredient: {ItemName} (ID: {ItemId}) x{Quantity}", 
                    ingredient.ItemName, string.IsNullOrEmpty(ingredient.ItemId) ? "[EMPTY]" : ingredient.ItemId, ingredient.Quantity);

                // Check if there's a recipe that creates this item
                var recipeForIngredient = await context.CachedCraftingRecipes
                    .Include(r => r.Ingredients)
                    .FirstOrDefaultAsync(r => r.OutputItemId == ingredient.ItemId);
                
                // Skip purification/refinement recipes - those are for processing raw materials, not crafting
                if (recipeForIngredient != null && recipeForIngredient.Name.Contains("Purification", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("  → {ItemName} is a purification recipe output (raw material) - skipping", ingredient.ItemName);
                    AddRawMaterial(rawMaterials, ingredient.ItemName, ingredient.Quantity);
                    continue;
                }
                
                if (recipeForIngredient != null && recipeForIngredient.Ingredients.Any())
                {
                    _logger.LogWarning("  ✓ Found recipe for {ItemName} in cache - {ProfessionName} Lvl {Level}", 
                        ingredient.ItemName, recipeForIngredient.Profession, recipeForIngredient.ProfessionLevel);
                    
                    // Track this profession as required
                    if (!string.IsNullOrEmpty(recipeForIngredient.Profession) && depth > 0)  // Don't track the initial recipe's profession
                    {
                        var professionLevel = GetProfessionLevelName(recipeForIngredient.Profession, recipeForIngredient.ProfessionLevel);
                        requiredProfessions?.Add($"{recipeForIngredient.Profession} - {professionLevel}");
                    }
                    
                    // Recurse into this recipe's ingredients, multiplying quantities by parent quantity
                    var subIngredients = recipeForIngredient.Ingredients
                        .Where(sub => sub.ItemId != ingredient.ItemId)  // Filter out self-referential ingredients
                        .Select(sub => new CachedRecipeIngredient
                        {
                            ItemId = sub.ItemId,
                            ItemName = sub.ItemName,
                            Quantity = sub.Quantity * ingredient.Quantity  // Multiply by parent quantity
                        })
                        .ToList();
                    
                    if (subIngredients.Any(sub => sub.ItemId != ingredient.ItemId))
                    {
                        _logger.LogWarning("  Recursing into {Count} sub-ingredients for {ItemName} (quantity multiplier: {ParentQty})", 
                            subIngredients.Count, ingredient.ItemName, ingredient.Quantity);
                        await FetchRawMaterialsRecursiveAsync(context, subIngredients, rawMaterials, apiService, depth + 1, maxDepth, requiredProfessions);
                        continue;
                    }
                    else
                    {
                        _logger.LogWarning("  → {ItemName} recipe only contains itself (circular) - treating as raw material", ingredient.ItemName);
                    }
                }

                // Not in cache - try fetching from API as fallback
                if (!string.IsNullOrEmpty(ingredient.ItemId))
                {
                    _logger.LogWarning("  ↻ Not in cache, checking API for recipe for {ItemName}...", ingredient.ItemName);
                    
                    var apiJsonString = await apiService.GetRecipeForItemAsync(ingredient.ItemId);
                    if (!string.IsNullOrEmpty(apiJsonString))
                    {
                        try
                        {
                            var itemJson = JsonDocument.Parse(apiJsonString).RootElement;
                            _logger.LogWarning("  ✓ Found recipe for {ItemName} via API - enriching...", ingredient.ItemName);
                            
                            // Extract ingredients from API response
                            var subIngredients = new List<CachedRecipeIngredient>();
                            if (itemJson.TryGetProperty("createdByRecipes", out var recipes) && recipes.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                var recipeArray = recipes.EnumerateArray().FirstOrDefault();
                                if (recipeArray.ValueKind != System.Text.Json.JsonValueKind.Undefined && 
                                    recipeArray.TryGetProperty("inputs", out var inputs))
                                {
                                    foreach (var inputGroup in inputs.EnumerateArray())
                                    {
                                        if (inputGroup.TryGetProperty("items", out var items))
                                        {
                                            foreach (var item in items.EnumerateArray())
                                            {
                                                var itemId = item.TryGetProperty("id", out var id) ? id.GetString() : null;
                                                var itemName = item.TryGetProperty("name", out var name) ? name.GetString() : "Unknown";
                                                var quantity = item.TryGetProperty("quantity", out var qty) ? qty.GetInt32() : 1;
                                                
                                                _logger.LogWarning("    API Recipe Input: {SubItemName} (ID: {SubItemId}) x{SubQty}", 
                                                    itemName, itemId ?? "[NULL]", quantity);
                                                
                                                if (!string.IsNullOrEmpty(itemId))
                                                {
                                                    subIngredients.Add(new CachedRecipeIngredient
                                                    {
                                                        ItemId = itemId,
                                                        ItemName = itemName ?? "Unknown",
                                                        Quantity = quantity * ingredient.Quantity
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            
                            if (subIngredients.Any())
                            {
                                _logger.LogWarning("  Recursing into {Count} sub-ingredients from API for {ItemName} (quantity multiplier: {ParentQty})", 
                                    subIngredients.Count, ingredient.ItemName, ingredient.Quantity);
                                await FetchRawMaterialsRecursiveAsync(context, subIngredients, rawMaterials, apiService, depth + 1, maxDepth, requiredProfessions);
                                continue;
                            }
                        }
                        catch (Exception apiEx)
                        {
                            _logger.LogWarning(apiEx, "Error parsing API recipe for {ItemName}", ingredient.ItemName);
                        }
                    }
                }

                _logger.LogWarning("  → {ItemName} is a raw material (no recipe found)", ingredient.ItemName);
                // No recipe found - treat as raw material
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
