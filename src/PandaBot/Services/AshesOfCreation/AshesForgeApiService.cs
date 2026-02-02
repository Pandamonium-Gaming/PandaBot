using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Models.AshesOfCreation;
using PandaBot.Utils;

namespace PandaBot.Services.AshesOfCreation;

public class AshesForgeApiService
{
    private readonly HttpClient _httpClient;
    private readonly PandaBotContext _context;
    private readonly IMemoryCache _cache;
    private readonly ImageCacheService _imageCache;
    private readonly ILogger<AshesForgeApiService> _logger;

    public AshesForgeApiService(
        IHttpClientFactory httpClientFactory,
        PandaBotContext context,
        IMemoryCache cache,
        ImageCacheService imageCache,
        ILogger<AshesForgeApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AshesForgeApi");
        _context = context;
        _cache = cache;
        _imageCache = imageCache;
        _logger = logger;
    }

    public async Task<CachedItem?> FetchAndCacheItemAsync(string itemName)
    {
        var items = await FetchItemsByNameAsync(itemName);
        return items.FirstOrDefault();
    }

    public async Task<List<CachedItem>> FetchItemsByNameAsync(string itemName)
    {
        try
        {
            // Use a timeout to prevent hanging
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            var url = $"items";
            _logger.LogInformation("Fetching all items from API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API request failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return new List<CachedItem>();
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            
            // Check if response is literally "null"
            if (json == "null" || string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("API returned null or empty response");
                return new List<CachedItem>();
            }
            
            _logger.LogInformation("API Response length: {Length} chars, first 500: {Json}", 
                json.Length, json.Length > 500 ? json[..500] : json);
            
            // Try to parse as different possible response formats
            List<JsonElement>? itemsJson = null;
            
            try
            {
                var jsonDoc = JsonDocument.Parse(json);
                
                // Check root element type
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    // Direct array
                    itemsJson = JsonSerializer.Deserialize<List<JsonElement>>(json);
                }
                else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    // Try common property names
                    if (jsonDoc.RootElement.TryGetProperty("data", out var dataProperty))
                    {
                        itemsJson = JsonSerializer.Deserialize<List<JsonElement>>(dataProperty.GetRawText());
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("items", out var itemsProperty))
                    {
                        itemsJson = JsonSerializer.Deserialize<List<JsonElement>>(itemsProperty.GetRawText());
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("results", out var resultsProperty))
                    {
                        itemsJson = JsonSerializer.Deserialize<List<JsonElement>>(resultsProperty.GetRawText());
                    }
                    else
                    {
                        // Log all property names to help debug
                        var propertyNames = string.Join(", ", jsonDoc.RootElement.EnumerateObject().Select(p => p.Name));
                        _logger.LogInformation("API response object properties: {Properties}", propertyNames);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse API response structure. Raw response: {Json}", 
                    json.Length > 1000 ? json[..1000] : json);
                return new List<CachedItem>();
            }

            if (itemsJson == null || itemsJson.Count == 0)
            {
                _logger.LogInformation("No items found in API response");
                return new List<CachedItem>();
            }

            _logger.LogInformation("Found {Count} item(s) in API response", itemsJson.Count);
            
            // Filter items by name locally
            var items = new List<CachedItem>();
            foreach (var itemJson in itemsJson)
            {
                try
                {
                    var item = ParseItemFromJson(itemJson, itemJson.GetRawText());
                    
                    // Filter by search query
                    if (!item.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    // Check if item already exists
                    var existingItem = await _context.CachedItems
                        .FirstOrDefaultAsync(i => i.ItemId == item.ItemId);
                    
                    if (existingItem != null)
                    {
                        items.Add(existingItem);
                        continue;
                    }
                    
                    // Cache new item
                    items.Add(item);
                    _context.CachedItems.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse item from JSON");
                }
            }
            
            if (items.Any(i => _context.Entry(i).State == EntityState.Added))
            {
                await _context.SaveChangesAsync();
                var newCount = items.Count(i => _context.Entry(i).State == EntityState.Unchanged);
                _logger.LogInformation("Cached {Count} new item(s)", newCount);
            }

            return items;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("API request timed out after 5 seconds");
            return new List<CachedItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching items '{ItemName}' from API", itemName);
            return new List<CachedItem>();
        }
    }

    public async Task<List<CachedItem>> FetchAllItemsAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            var url = $"items";
            _logger.LogInformation("Fetching all items from API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API request failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return new List<CachedItem>();
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            
            if (json == "null" || string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("API returned null or empty response");
                return new List<CachedItem>();
            }
            
            _logger.LogInformation("API Response length: {Length} chars", json.Length);
            
            var itemsJson = JsonSerializer.Deserialize<List<JsonElement>>(json);

            if (itemsJson == null || itemsJson.Count == 0)
            {
                _logger.LogInformation("No items found in API response");
                return new List<CachedItem>();
            }

            _logger.LogInformation("Found {Count} item(s) in API response, processing...", itemsJson.Count);
            
            var items = new List<CachedItem>();
            var addedCount = 0;
            
            foreach (var itemJson in itemsJson)
            {
                try
                {
                    var item = ParseItemFromJson(itemJson, itemJson.GetRawText());
                    
                    var existingItem = await _context.CachedItems
                        .FirstOrDefaultAsync(i => i.ItemId == item.ItemId);
                    
                    if (existingItem != null)
                    {
                        items.Add(existingItem);
                        continue;
                    }
                    
                    items.Add(item);
                    _context.CachedItems.Add(item);
                    addedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse item from JSON");
                }
            }
            
            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cached {Count} new item(s)", addedCount);
            }

            return items;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("API request timed out after 30 seconds");
            return new List<CachedItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all items from API");
            return new List<CachedItem>();
        }
    }

    public async Task<List<CachedCraftingRecipe>> FetchAllRecipesAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            var url = $"crafting-recipes";
            _logger.LogInformation("Fetching all recipes from API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API request failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return new List<CachedCraftingRecipe>();
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            
            if (json == "null" || string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("API returned null or empty response");
                return new List<CachedCraftingRecipe>();
            }
            
            _logger.LogInformation("API Response length: {Length} chars, first 500: {Preview}", 
                json.Length, json.Length > 500 ? json[..500] : json);
            
            var recipesJson = JsonSerializer.Deserialize<List<JsonElement>>(json);

            if (recipesJson == null || recipesJson.Count == 0)
            {
                _logger.LogInformation("No recipes found in API response");
                return new List<CachedCraftingRecipe>();
            }

            _logger.LogInformation("Found {Count} recipe(s) in API response, processing...", recipesJson.Count);
            _logger.LogWarning("Note: AshesForge API does not provide ingredient data in the /api/crafting-recipes endpoint. Recipes will be cached without ingredients.");
            
            var recipes = new List<CachedCraftingRecipe>();
            var addedCount = 0;
            
            foreach (var recipeJson in recipesJson)
            {
                try
                {
                    var rawJson = recipeJson.GetRawText();
                    var recipe = ParseRecipeFromJson(recipeJson, rawJson);
                    
                    var existingRecipe = await _context.CachedCraftingRecipes
                        .FirstOrDefaultAsync(r => r.RecipeId == recipe.RecipeId);
                    
                    if (existingRecipe != null)
                    {
                        recipes.Add(existingRecipe);
                        continue;
                    }
                    
                    _context.CachedCraftingRecipes.Add(recipe);
                    recipes.Add(recipe);
                    addedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse recipe from JSON");
                }
            }
            
            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cached {RecipeCount} new recipe(s)", addedCount);
            }

            return recipes;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("API request timed out after 30 seconds");
            return new List<CachedCraftingRecipe>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all recipes from API");
            return new List<CachedCraftingRecipe>();
        }
    }

    public async Task<List<CachedMob>> FetchAllMobsAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            var url = $"mobs";
            _logger.LogInformation("Fetching all mobs from API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API request failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return new List<CachedMob>();
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            
            if (json == "null" || string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("API returned null or empty response");
                return new List<CachedMob>();
            }
            
            _logger.LogInformation("API Response length: {Length} chars", json.Length);
            
            var mobsJson = JsonSerializer.Deserialize<List<JsonElement>>(json);

            if (mobsJson == null || mobsJson.Count == 0)
            {
                _logger.LogInformation("No mobs found in API response");
                return new List<CachedMob>();
            }

            _logger.LogInformation("Found {Count} mob(s) in API response, processing...", mobsJson.Count);
            
            var mobs = new List<CachedMob>();
            var addedCount = 0;
            
            foreach (var mobJson in mobsJson)
            {
                try
                {
                    var mob = ParseMobFromJson(mobJson, mobJson.GetRawText());
                    
                    var existingMob = await _context.CachedMobs
                        .FirstOrDefaultAsync(m => m.MobId == mob.MobId);
                    
                    if (existingMob != null)
                    {
                        mobs.Add(existingMob);
                        continue;
                    }
                    
                    mobs.Add(mob);
                    _context.CachedMobs.Add(mob);
                    addedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse mob from JSON");
                }
            }
            
            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cached {Count} new mob(s)", addedCount);
            }

            return mobs;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("API request timed out after 30 seconds");
            return new List<CachedMob>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all mobs from API");
            return new List<CachedMob>();
        }
    }

    public async Task<JsonElement?> FetchItemDetailsAsync(string itemId)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            var url = $"items/{itemId}";
            _logger.LogDebug("Fetching item details from API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Item details request failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            
            if (json == "null" || string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            
            var itemDetails = JsonSerializer.Deserialize<JsonElement>(json);
            return itemDetails;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching item details for {ItemId}", itemId);
            return null;
        }
    }

    /// <summary>
    /// Enriches a single recipe with ingredient data on-the-fly
    /// </summary>
    public async Task EnrichSingleRecipeAsync(CachedCraftingRecipe recipe)
    {
        if (string.IsNullOrEmpty(recipe.OutputItemId))
        {
            _logger.LogWarning("Cannot enrich recipe {RecipeName} - no OutputItemId", recipe.Name);
            return;
        }

        _logger.LogWarning("=== On-the-fly enrichment for {RecipeName} (ItemId: {ItemId}) ===", 
            recipe.Name, recipe.OutputItemId);

        try
        {
            var itemDetails = await FetchItemDetailsAsync(recipe.OutputItemId);
            
            if (!itemDetails.HasValue)
            {
                _logger.LogWarning("Failed to fetch item details for {RecipeName}", recipe.Name);
                return;
            }
            
            var item = itemDetails.Value;
            
            // Save the output item with its stats to cache
            var itemId = JsonHelper.GetStringProperty(item, "id") ?? recipe.OutputItemId;
            var itemName = JsonHelper.GetStringProperty(item, "name") ?? recipe.OutputItemName;
            var iconUrl = JsonHelper.GetStringProperty(item, "icon") ?? string.Empty;
            var rarity = JsonHelper.GetStringProperty(item, "rarity") ?? string.Empty;
            var rawJson = item.GetRawText();
            
            _logger.LogWarning("Caching output item {ItemName} with stats", itemName);
            
            var existingItem = await _context.CachedItems.FirstOrDefaultAsync(i => i.ItemId == itemId);
            if (existingItem != null)
            {
                existingItem.Name = itemName;
                existingItem.IconUrl = iconUrl;
                existingItem.Rarity = rarity;
                existingItem.RawJson = rawJson;
                existingItem.LastUpdated = DateTime.UtcNow;
                _context.CachedItems.Update(existingItem);
            }
            else
            {
                _context.CachedItems.Add(new CachedItem
                {
                    ItemId = itemId,
                    Name = itemName,
                    IconUrl = iconUrl,
                    Rarity = rarity,
                    RawJson = rawJson,
                    CachedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                });
            }
            
            // Save the output item (always save, even if no recipe found)
            await _context.SaveChangesAsync();
            _logger.LogWarning("Saved output item {ItemName} with RawJson", itemName);
            
            // Recipe data is in createdByRecipes array
            if (item.TryGetProperty("createdByRecipes", out var recipesProperty) && 
                recipesProperty.ValueKind == JsonValueKind.Array)
            {
                var recipesArray = recipesProperty.EnumerateArray().ToList();
                if (recipesArray.Count > 0)
                {
                    // Use the first recipe in the array
                    var recipeData = recipesArray[0];
                    
                    if (recipeData.TryGetProperty("profession", out var profProperty) &&
                        recipeData.TryGetProperty("certificationLevel", out var levelProperty) &&
                        recipeData.TryGetProperty("inputs", out var inputsProperty) &&
                        inputsProperty.ValueKind == JsonValueKind.Array)
                    {
                        // Extract profession and level names
                        var apiProfessionName = string.Empty;
                        if (profProperty.ValueKind == JsonValueKind.String)
                            apiProfessionName = profProperty.GetString() ?? string.Empty;
                        else if (profProperty.ValueKind == JsonValueKind.Object)
                            apiProfessionName = JsonHelper.GetStringProperty(profProperty, "name") ?? string.Empty;
                        
                        var apiLevelName = string.Empty;
                        if (levelProperty.ValueKind == JsonValueKind.String)
                            apiLevelName = levelProperty.GetString() ?? string.Empty;
                        else if (levelProperty.ValueKind == JsonValueKind.Object)
                            apiLevelName = JsonHelper.GetStringProperty(levelProperty, "name") ?? string.Empty;
                        
                        if (!string.IsNullOrEmpty(apiProfessionName) && !string.IsNullOrEmpty(apiLevelName))
                        {
                            _logger.LogWarning("Found recipe - {Profession} {Level}", apiProfessionName, apiLevelName);
                            
                            int ingredientCount = 0;
                            foreach (var inputGroup in inputsProperty.EnumerateArray())
                            {
                                var groupQuantity = JsonHelper.GetIntProperty(inputGroup, "quantity") ?? 1;
                                
                                if (inputGroup.TryGetProperty("items", out var itemsArray) && 
                                    itemsArray.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var ingredientJson in itemsArray.EnumerateArray())
                                    {
                                        var ingredient = new CachedRecipeIngredient
                                        {
                                            CachedCraftingRecipeId = recipe.Id,
                                            ItemId = JsonHelper.GetStringProperty(ingredientJson, "id") ?? 
                                                    JsonHelper.GetStringProperty(ingredientJson, "itemId") ?? string.Empty,
                                            ItemName = JsonHelper.GetStringProperty(ingredientJson, "name") ?? 
                                                      JsonHelper.GetStringProperty(ingredientJson, "itemName") ?? string.Empty,
                                            Quantity = groupQuantity
                                        };
                                        
                                        if (!string.IsNullOrEmpty(ingredient.ItemId) || !string.IsNullOrEmpty(ingredient.ItemName))
                                        {
                                            _context.CachedRecipeIngredients.Add(ingredient);
                                            ingredientCount++;
                                        }
                                    }
                                }
                            }
                            
                            if (ingredientCount > 0)
                            {
                                await _context.SaveChangesAsync();
                                _logger.LogWarning("Enriched {RecipeName} with {Count} ingredients", recipe.Name, ingredientCount);
                                
                                // Now fetch and cache each ingredient item for raw materials recursion
                                _logger.LogWarning("Fetching ingredient details for raw materials recursion...");
                                var ingredientsToFetch = await _context.CachedRecipeIngredients
                                    .Where(i => i.CachedCraftingRecipeId == recipe.Id)
                                    .ToListAsync();
                                
                                foreach (var ingredient in ingredientsToFetch)
                                {
                                    if (string.IsNullOrEmpty(ingredient.ItemId))
                                        continue;
                                    
                                    // Fetch ingredient details - always refresh to ensure we have createdByRecipes
                                    _logger.LogWarning("  Fetching ingredient {ItemName} ({ItemId})...", ingredient.ItemName, ingredient.ItemId);
                                    var ingredientDetails = await FetchItemDetailsAsync(ingredient.ItemId);
                                    
                                    if (ingredientDetails.HasValue)
                                    {
                                        var ingredientItem = ingredientDetails.Value;
                                        var ingredientName = JsonHelper.GetStringProperty(ingredientItem, "name") ?? ingredient.ItemName;
                                        var ingredientIconUrl = JsonHelper.GetStringProperty(ingredientItem, "icon") ?? string.Empty;
                                        var ingredientRarity = JsonHelper.GetStringProperty(ingredientItem, "rarity") ?? string.Empty;
                                        var ingredientRawJson = ingredientItem.GetRawText();
                                        
                                        // Check if already exists and update, or add new
                                        var cachedIngredientItem = await _context.CachedItems
                                            .FirstOrDefaultAsync(i => i.ItemId == ingredient.ItemId);
                                        
                                        if (cachedIngredientItem != null)
                                        {
                                            cachedIngredientItem.Name = ingredientName;
                                            cachedIngredientItem.IconUrl = ingredientIconUrl;
                                            cachedIngredientItem.Rarity = ingredientRarity;
                                            cachedIngredientItem.RawJson = ingredientRawJson;
                                            cachedIngredientItem.LastUpdated = DateTime.UtcNow;
                                            _context.CachedItems.Update(cachedIngredientItem);
                                            _logger.LogWarning("    ✓ Updated cached ingredient {ItemName}", ingredientName);
                                        }
                                        else
                                        {
                                            _context.CachedItems.Add(new CachedItem
                                            {
                                                ItemId = ingredient.ItemId,
                                                Name = ingredientName,
                                                IconUrl = ingredientIconUrl,
                                                Rarity = ingredientRarity,
                                                RawJson = ingredientRawJson,
                                                CachedAt = DateTime.UtcNow,
                                                LastUpdated = DateTime.UtcNow
                                            });
                                            _logger.LogWarning("    ✓ Added ingredient to cache {ItemName}", ingredientName);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("    ✗ Failed to fetch ingredient {ItemName}", ingredient.ItemName);
                                    }
                                }
                                
                                // Save all cached ingredient items
                                if (ingredientsToFetch.Count > 0)
                                {
                                    await _context.SaveChangesAsync();
                                    _logger.LogWarning("Cached {Count} ingredient items with full details", ingredientsToFetch.Count);
                                }
                            }
                            return;
                        }
                    }
                }
            }
            
            _logger.LogWarning("Could not find recipe data for {RecipeName}", recipe.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enriching {RecipeName}", recipe.Name);
        }
    }

    public async Task EnrichRecipesWithIngredientsAsync()
    {
        _logger.LogWarning("=== Starting recipe enrichment process ===");
        _logger.LogWarning("Enriching recipes with ingredient data from item details...");
        _logger.LogWarning("This process fetches individual item details and uses aggressive rate limiting to avoid overloading the API.");
        
        var recipes = await _context.CachedCraftingRecipes
            .Include(r => r.Ingredients)
            .Where(r => !r.Ingredients.Any()) // Only recipes without ingredients
            .ToListAsync();
        
        _logger.LogWarning("Found {Count} recipes without ingredients - starting enrichment", recipes.Count);
        
        var enrichedCount = 0;
        var ingredientCount = 0;
        var failedCount = 0;
        var batchSize = 50; // Smaller batches
        var requestDelay = 1000; // 1 second between requests
        var batchDelay = 5000; // 5 seconds between batches
        
        for (int i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            
            if (string.IsNullOrEmpty(recipe.OutputItemId))
            {
                _logger.LogWarning("Recipe {RecipeName} ({RecipeId}) has no OutputItemId", recipe.Name, recipe.RecipeId);
                failedCount++;
                continue;
            }
            
            try
            {
                // Rate limiting: 1 request per second
                await Task.Delay(requestDelay);
                
                var itemDetails = await FetchItemDetailsAsync(recipe.OutputItemId);
                
                if (!itemDetails.HasValue)
                {
                    _logger.LogWarning("Failed to fetch item details for recipe {RecipeName} (OutputItemId: {ItemId})", 
                        recipe.Name, recipe.OutputItemId);
                    failedCount++;
                    continue;
                }
                
                var item = itemDetails.Value;
                _logger.LogWarning("Fetched item for recipe {RecipeName}: {ItemId}", recipe.Name, recipe.OutputItemId);
                
                // The API may return the recipe directly on the item if it's crafted
                // Recipe data is in createdByRecipes array
                if (item.TryGetProperty("createdByRecipes", out var recipesProperty) && 
                    recipesProperty.ValueKind == JsonValueKind.Array)
                {
                    var recipesArray = recipesProperty.EnumerateArray().ToList();
                    if (recipesArray.Count > 0)
                    {
                        var recipeData = recipesArray[0];
                        
                        if (recipeData.TryGetProperty("profession", out var profProperty) &&
                            recipeData.TryGetProperty("certificationLevel", out var levelProperty) &&
                            recipeData.TryGetProperty("inputs", out var inputsProperty) &&
                            inputsProperty.ValueKind == JsonValueKind.Array)
                        {
                            // Extract profession name - could be a string or an object with .name
                            var apiProfessionName = string.Empty;
                            if (profProperty.ValueKind == JsonValueKind.String)
                            {
                                apiProfessionName = profProperty.GetString() ?? string.Empty;
                            }
                            else if (profProperty.ValueKind == JsonValueKind.Object)
                            {
                                apiProfessionName = JsonHelper.GetStringProperty(profProperty, "name") ?? string.Empty;
                            }
                            
                            // Extract level name - could be a string or an object with .name
                            var apiLevelName = string.Empty;
                            if (levelProperty.ValueKind == JsonValueKind.String)
                            {
                                apiLevelName = levelProperty.GetString() ?? string.Empty;
                            }
                            else if (levelProperty.ValueKind == JsonValueKind.Object)
                            {
                                apiLevelName = JsonHelper.GetStringProperty(levelProperty, "name") ?? string.Empty;
                            }
                            
                            _logger.LogWarning("Found recipe data for {RecipeName} - Profession: {Profession}, Level: {Level}", 
                                recipe.Name, apiProfessionName, apiLevelName);
                            
                            // Parse ingredients directly from the recipe's inputs array
                            var ontheflyIngredientCount = 0;
                            foreach (var inputGroup in inputsProperty.EnumerateArray())
                            {
                                // Get quantity from the input group level
                                var groupQuantity = JsonHelper.GetIntProperty(inputGroup, "quantity") ?? 1;
                                
                                // Each input group has an "items" array
                                if (inputGroup.TryGetProperty("items", out var itemsArray) && 
                                    itemsArray.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var ingredientJson in itemsArray.EnumerateArray())
                                    {
                                        var ingredient = new CachedRecipeIngredient
                                        {
                                            CachedCraftingRecipeId = recipe.Id,
                                            ItemId = JsonHelper.GetStringProperty(ingredientJson, "id") ?? 
                                                    JsonHelper.GetStringProperty(ingredientJson, "itemId") ?? string.Empty,
                                            ItemName = JsonHelper.GetStringProperty(ingredientJson, "name") ?? 
                                                      JsonHelper.GetStringProperty(ingredientJson, "itemName") ?? string.Empty,
                                            Quantity = groupQuantity
                                        };
                                        
                                        if (!string.IsNullOrEmpty(ingredient.ItemId) || !string.IsNullOrEmpty(ingredient.ItemName))
                                        {
                                            _context.CachedRecipeIngredients.Add(ingredient);
                                            ontheflyIngredientCount++;
                                        }
                                    }
                                }
                            }
                            
                            await _context.SaveChangesAsync();
                            _logger.LogWarning("Background enriched recipe {RecipeName} with {Count} ingredients", 
                                recipe.Name, ontheflyIngredientCount);
                            enrichedCount++;
                            ingredientCount += ontheflyIngredientCount;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Recipe {RecipeName} - no createdByRecipes found in item data", recipe.Name);
                    failedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enriching recipe {RecipeName}", recipe.Name);
                failedCount++;
            }
            
            // Save in batches and add delay between batches
            if ((i + 1) % batchSize == 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogWarning("Progress: {Current}/{Total} recipes processed ({Percent}%), {Enriched} enriched, {Ingredients} ingredients added. Pausing for {Delay}s...", 
                    i + 1, recipes.Count, ((i + 1) * 100 / recipes.Count), enrichedCount, ingredientCount, batchDelay / 1000);
                
                // Longer pause between batches to be extra respectful
                await Task.Delay(batchDelay);
            }
        }
        
        // Save remaining changes
        await _context.SaveChangesAsync();
        
        _logger.LogWarning("=== Recipe enrichment complete ===");
        _logger.LogWarning("Results: {Enriched} recipes enriched with {Ingredients} total ingredients ({Failed} failed)", 
            enrichedCount, ingredientCount, failedCount);
        _logger.LogInformation("Total API calls made: {Count} over approximately {Minutes} minutes", 
            enrichedCount, (enrichedCount * requestDelay / 1000 / 60));
    }

    private CachedItem ParseItemFromJson(JsonElement itemJson, string rawJson)
    {
        var tags = itemJson.TryGetProperty("tags", out var tagsProperty) 
            ? tagsProperty.GetRawText() 
            : "[]";

        return new CachedItem
        {
            ItemId = JsonHelper.GetStringProperty(itemJson, "id") ?? Guid.NewGuid().ToString(),
            Name = JsonHelper.GetStringProperty(itemJson, "name") ?? "Unknown",
            Description = JsonHelper.GetStringProperty(itemJson, "description") ?? string.Empty,
            Type = ExtractTypeFromTags(tags),
            Category = ExtractCategoryFromTags(tags),
            Rarity = JsonHelper.GetStringProperty(itemJson, "rarity") ?? "Common",
            Level = JsonHelper.GetIntProperty(itemJson, "level"),
            IconUrl = JsonHelper.GetStringProperty(itemJson, "icon") ?? string.Empty,
            ImageUrl = JsonHelper.GetStringProperty(itemJson, "image") ?? string.Empty,
            IsStackable = JsonHelper.GetBoolProperty(itemJson, "stackable"),
            MaxStackSize = JsonHelper.GetIntProperty(itemJson, "maxStack"),
            SlotType = JsonHelper.GetStringProperty(itemJson, "slot") ?? string.Empty,
            Enchantable = JsonHelper.GetBoolProperty(itemJson, "enchantable"),
            VendorValueType = JsonHelper.GetStringProperty(itemJson, "vendorValueType") ?? string.Empty,
            Views = JsonHelper.GetIntProperty(itemJson, "views") ?? 0,
            Tags = tags,
            RawJson = rawJson,
            CachedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
    }

    private string ExtractTypeFromTags(string tagsJson)
    {
        try
        {
            var tags = JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>();
            
            // Look for specific type tags
            foreach (var tag in tags)
            {
                if (tag.StartsWith("Item.") && tag.Split('.').Length >= 2)
                {
                    var parts = tag.Split('.');
                    if (parts.Length >= 3)
                        return parts[2]; // e.g., "Item.Resource.Raw" -> "Raw"
                }
            }
            
            return tags.FirstOrDefault(t => t != "item" && t.StartsWith("Item."))?.Replace("Item.", "") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ExtractCategoryFromTags(string tagsJson)
    {
        try
        {
            var tags = JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>();
            
            // Look for category-like tags
            var categoryTag = tags.FirstOrDefault(t => 
                t.StartsWith("Item.") && t.Split('.').Length >= 2);
            
            if (categoryTag != null)
            {
                var parts = categoryTag.Split('.');
                if (parts.Length >= 2)
                    return parts[1]; // e.g., "Item.Resource" -> "Resource"
            }
            
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private int GetProfessionLevelFromName(string levelName)
    {
        return levelName switch
        {
            "Novice" => 0,
            "Apprentice" => 1,
            "Journeyman" => 2,
            "Artisan" => 3,
            "Master" => 4,
            "Expert" => 5,
            "Legendary" => 4,
            "Ancient" => 5,
            _ => 0
        };
    }

    private int GetProfessionLevel(JsonElement recipeJson)
    {
        // Try direct integer properties first
        if (JsonHelper.GetIntProperty(recipeJson, "professionLevel") is int profLevel)
            return profLevel;
        
        if (JsonHelper.GetIntProperty(recipeJson, "level") is int level)
            return level;
        
        // Try certificationLevel as object with name property
        if (recipeJson.TryGetProperty("certificationLevel", out var certLevel))
        {
            if (certLevel.ValueKind == JsonValueKind.Object && certLevel.TryGetProperty("name", out var certName))
            {
                var levelName = JsonHelper.GetStringProperty(certLevel, "name") ?? string.Empty;
                return GetProfessionLevelFromName(levelName);
            }
            else if (certLevel.ValueKind == JsonValueKind.String)
            {
                var levelName = certLevel.GetString() ?? string.Empty;
                return GetProfessionLevelFromName(levelName);
            }
            else if (certLevel.ValueKind == JsonValueKind.Number && certLevel.TryGetInt32(out var intVal))
            {
                return intVal;
            }
        }
        
        return 0;
    }

    private string GetLevelNameFromNumber(int levelNumber)
    {
        return levelNumber switch
        {
            1 => "Novice",
            2 => "Apprentice",
            3 => "Journeyman",
            4 => "Artisan",
            5 => "Master",
            6 => "Expert",
            7 => "Grandmaster",
            _ => "Unknown"
        };
    }

    private CachedCraftingRecipe ParseRecipeFromJson(JsonElement recipeJson, string rawJson)
    {
        // Don't log warnings for every recipe - we know ingredients aren't provided
        var ingredients = new List<CachedRecipeIngredient>();
        
        // Parse ingredients array - try multiple possible field names
        var ingredientFields = new[] { "ingredients", "inputs", "materials", "requirements", "components" };
        
        foreach (var fieldName in ingredientFields)
        {
            if (recipeJson.TryGetProperty(fieldName, out var ingredientsProperty) && 
                ingredientsProperty.ValueKind == JsonValueKind.Array)
            {
                _logger.LogDebug("Found ingredients in field '{Field}'", fieldName);
                
                foreach (var ingredientJson in ingredientsProperty.EnumerateArray())
                {
                    var itemId = JsonHelper.GetStringProperty(ingredientJson, "id") ?? 
                                 JsonHelper.GetStringProperty(ingredientJson, "itemId") ?? 
                                 string.Empty;
                    
                    var itemName = JsonHelper.GetStringProperty(ingredientJson, "name") ?? 
                                   JsonHelper.GetStringProperty(ingredientJson, "itemName") ?? 
                                   string.Empty;
                    
                    var quantity = JsonHelper.GetIntProperty(ingredientJson, "quantity") ?? 
                                   JsonHelper.GetIntProperty(ingredientJson, "amount") ?? 
                                   JsonHelper.GetIntProperty(ingredientJson, "count") ?? 
                                   1;
                    
                    if (!string.IsNullOrEmpty(itemId) || !string.IsNullOrEmpty(itemName))
                    {
                        ingredients.Add(new CachedRecipeIngredient
                        {
                            ItemId = itemId,
                            ItemName = itemName,
                            Quantity = quantity
                        });
                    }
                }
                break;
            }
        }

        // Parse output item info
        var outputItemId = string.Empty;
        var outputItemName = string.Empty;
        
        if (recipeJson.TryGetProperty("outputs", out var outputsProperty) && 
            outputsProperty.ValueKind == JsonValueKind.Array)
        {
            var firstOutput = outputsProperty.EnumerateArray().FirstOrDefault();
            if (firstOutput.ValueKind != JsonValueKind.Undefined)
            {
                outputItemId = JsonHelper.GetStringProperty(firstOutput, "id") ?? string.Empty;
                outputItemName = JsonHelper.GetStringProperty(firstOutput, "name") ?? string.Empty;
            }
        }

        var recipe = new CachedCraftingRecipe
        {
            RecipeId = JsonHelper.GetStringProperty(recipeJson, "id") ?? Guid.NewGuid().ToString(),
            Name = JsonHelper.GetStringProperty(recipeJson, "name") ?? "Unknown Recipe",
            Profession = JsonHelper.GetStringProperty(recipeJson, "profession") ?? string.Empty,
            ProfessionLevel = GetProfessionLevel(recipeJson),
            CertificationLevel = JsonHelper.GetStringProperty(recipeJson, "certificationLevel") ?? string.Empty,
            OutputItemId = outputItemId,
            OutputItemName = outputItemName,
            OutputQuantity = JsonHelper.GetIntProperty(recipeJson, "outputQuantity") ?? 
                            JsonHelper.GetIntProperty(recipeJson, "quantity") ?? 1,
            Station = JsonHelper.GetStringProperty(recipeJson, "station") ?? string.Empty,
            CraftTime = JsonHelper.GetIntProperty(recipeJson, "craftTime") ?? 0,
            Views = JsonHelper.GetIntProperty(recipeJson, "views") ?? 0,
            RawJson = rawJson,
            CachedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Ingredients = ingredients
        };

        if (ingredients.Count == 0)
        {
            _logger.LogWarning("Recipe '{RecipeName}' has no ingredients - this might be expected or the API doesn't provide this data", recipe.Name);
        }

        return recipe;
    }

    private CachedMob ParseMobFromJson(JsonElement mobJson, string rawJson)
    {
        return new CachedMob
        {
            MobId = JsonHelper.GetStringProperty(mobJson, "id") ?? Guid.NewGuid().ToString(),
            Name = JsonHelper.GetStringProperty(mobJson, "name") ?? "Unknown Mob",
            Type = JsonHelper.GetStringProperty(mobJson, "type") ?? string.Empty,
            Category = JsonHelper.GetStringProperty(mobJson, "category") ?? string.Empty,
            Level = JsonHelper.GetIntProperty(mobJson, "level"),
            Location = JsonHelper.GetStringProperty(mobJson, "location") ?? string.Empty,
            ImageUrl = JsonHelper.GetStringProperty(mobJson, "image") ?? JsonHelper.GetStringProperty(mobJson, "icon") ?? string.Empty,
            IsBoss = JsonHelper.GetBoolProperty(mobJson, "isBoss") || JsonHelper.GetBoolProperty(mobJson, "boss"),
            IsElite = JsonHelper.GetBoolProperty(mobJson, "isElite") || JsonHelper.GetBoolProperty(mobJson, "elite"),
            RawJson = rawJson,
            CachedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Fetches recipe data for an item from the API (what creates this item)
    /// Returns the item details as raw JSON string which includes createdByRecipes array
    /// </summary>
    public async Task<string?> GetRecipeForItemAsync(string itemId)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            var url = $"items/{itemId}";
            _logger.LogDebug("Fetching item/recipe details from API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Item details request failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            
            if (json == "null" || string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching recipe for item {ItemId}", itemId);
            return null;
        }
    }
}

