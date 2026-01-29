using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Models.AshesOfCreation;

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

    public async Task EnrichRecipesWithIngredientsAsync()
    {
        _logger.LogInformation("Enriching recipes with ingredient data from item details...");
        _logger.LogWarning("This process fetches individual item details and uses aggressive rate limiting to avoid overloading the API.");
        
        var recipes = await _context.CachedCraftingRecipes
            .Include(r => r.Ingredients)
            .Where(r => !r.Ingredients.Any()) // Only recipes without ingredients
            .ToListAsync();
        
        _logger.LogInformation("Found {Count} recipes without ingredients", recipes.Count);
        
        var enrichedCount = 0;
        var ingredientCount = 0;
        var batchSize = 50; // Smaller batches
        var requestDelay = 1000; // 1 second between requests
        var batchDelay = 5000; // 5 seconds between batches
        
        for (int i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            
            if (string.IsNullOrEmpty(recipe.OutputItemId))
            {
                continue;
            }
            
            try
            {
                // Rate limiting: 1 request per second
                await Task.Delay(requestDelay);
                
                var itemDetails = await FetchItemDetailsAsync(recipe.OutputItemId);
                
                if (!itemDetails.HasValue)
                {
                    continue;
                }
                
                var item = itemDetails.Value;
                
                // Check for createdByRecipes
                if (item.TryGetProperty("createdByRecipes", out var recipesProperty) && 
                    recipesProperty.ValueKind == JsonValueKind.Array)
                {
                    foreach (var recipeDetail in recipesProperty.EnumerateArray())
                    {
                        var recipeId = GetStringProperty(recipeDetail, "id") ?? 
                                      GetStringProperty(recipeDetail, "recipeId") ?? string.Empty;
                        
                        // Check if this is the current recipe
                        if (recipeId != recipe.RecipeId)
                            continue;
                        
                        // Parse ingredients from inputs.items structure
                        if (recipeDetail.TryGetProperty("inputs", out var inputsProperty) && 
                            inputsProperty.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var inputGroup in inputsProperty.EnumerateArray())
                            {
                                // Each input group has an "items" array
                                if (inputGroup.TryGetProperty("items", out var itemsArray) && 
                                    itemsArray.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var ingredientJson in itemsArray.EnumerateArray())
                                    {
                                        var ingredient = new CachedRecipeIngredient
                                        {
                                            CachedCraftingRecipeId = recipe.Id,
                                            ItemId = GetStringProperty(ingredientJson, "id") ?? 
                                                    GetStringProperty(ingredientJson, "itemId") ?? string.Empty,
                                            ItemName = GetStringProperty(ingredientJson, "name") ?? 
                                                      GetStringProperty(ingredientJson, "itemName") ?? string.Empty,
                                            Quantity = GetIntProperty(ingredientJson, "quantity") ?? 
                                                      GetIntProperty(ingredientJson, "amount") ?? 1
                                        };
                                        
                                        // Try to parse quantityExpression if it's a string number
                                        if (ingredient.Quantity == 1)
                                        {
                                            if (ingredientJson.TryGetProperty("quantityExpression", out var qtyExpr) && 
                                                qtyExpr.ValueKind == JsonValueKind.String &&
                                                int.TryParse(qtyExpr.GetString(), out var qty))
                                            {
                                                ingredient.Quantity = qty;
                                            }
                                        }
                                        
                                        if (!string.IsNullOrEmpty(ingredient.ItemId) || !string.IsNullOrEmpty(ingredient.ItemName))
                                        {
                                            _context.CachedRecipeIngredients.Add(ingredient);
                                            ingredientCount++;
                                        }
                                    }
                                }
                            }
                            
                            enrichedCount++;
                            break;
                        }
                        
                        enrichedCount++;
                        break;
                    }
                }
                
                // Save in batches and add delay between batches
                if ((i + 1) % batchSize == 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Progress: {Current}/{Total} recipes processed ({Percent}%), {Enriched} enriched, {Ingredients} ingredients added. Pausing for {Delay}s...", 
                        i + 1, recipes.Count, ((i + 1) * 100 / recipes.Count), enrichedCount, ingredientCount, batchDelay / 1000);
                    
                    // Longer pause between batches to be extra respectful
                    await Task.Delay(batchDelay);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enriching recipe {RecipeName}", recipe.Name);
            }
        }
        
        // Save remaining changes
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Recipe enrichment complete: {Enriched} recipes enriched with {Ingredients} total ingredients", 
            enrichedCount, ingredientCount);
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
            ItemId = GetStringProperty(itemJson, "id") ?? Guid.NewGuid().ToString(),
            Name = GetStringProperty(itemJson, "name") ?? "Unknown",
            Description = GetStringProperty(itemJson, "description") ?? string.Empty,
            Type = ExtractTypeFromTags(tags),
            Category = ExtractCategoryFromTags(tags),
            Rarity = GetStringProperty(itemJson, "rarity") ?? "Common",
            Level = GetIntProperty(itemJson, "level"),
            IconUrl = GetStringProperty(itemJson, "icon") ?? string.Empty,
            ImageUrl = GetStringProperty(itemJson, "image") ?? string.Empty,
            IsStackable = GetBoolProperty(itemJson, "stackable"),
            MaxStackSize = GetIntProperty(itemJson, "maxStack"),
            SlotType = GetStringProperty(itemJson, "slot") ?? string.Empty,
            Enchantable = GetBoolProperty(itemJson, "enchantable"),
            VendorValueType = GetStringProperty(itemJson, "vendorValueType") ?? string.Empty,
            Views = GetIntProperty(itemJson, "views") ?? 0,
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

    private string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
            return property.GetString();
        return null;
    }

    private int? GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
            return property.GetInt32();
        return null;
    }

    private bool GetBoolProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.True)
            return true;
        return false;
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
                    var itemId = GetStringProperty(ingredientJson, "id") ?? 
                                 GetStringProperty(ingredientJson, "itemId") ?? 
                                 string.Empty;
                    
                    var itemName = GetStringProperty(ingredientJson, "name") ?? 
                                   GetStringProperty(ingredientJson, "itemName") ?? 
                                   string.Empty;
                    
                    var quantity = GetIntProperty(ingredientJson, "quantity") ?? 
                                   GetIntProperty(ingredientJson, "amount") ?? 
                                   GetIntProperty(ingredientJson, "count") ?? 
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
                outputItemId = GetStringProperty(firstOutput, "id") ?? string.Empty;
                outputItemName = GetStringProperty(firstOutput, "name") ?? string.Empty;
            }
        }

        var recipe = new CachedCraftingRecipe
        {
            RecipeId = GetStringProperty(recipeJson, "id") ?? Guid.NewGuid().ToString(),
            Name = GetStringProperty(recipeJson, "name") ?? "Unknown Recipe",
            Profession = GetStringProperty(recipeJson, "profession") ?? string.Empty,
            ProfessionLevel = GetIntProperty(recipeJson, "professionLevel") ?? 
                             GetIntProperty(recipeJson, "certificationLevel") ?? 
                             GetIntProperty(recipeJson, "level") ?? 0,
            OutputItemId = outputItemId,
            OutputItemName = outputItemName,
            OutputQuantity = GetIntProperty(recipeJson, "outputQuantity") ?? 
                            GetIntProperty(recipeJson, "quantity") ?? 1,
            Station = GetStringProperty(recipeJson, "station") ?? string.Empty,
            CraftTime = GetIntProperty(recipeJson, "craftTime") ?? 0,
            Views = GetIntProperty(recipeJson, "views") ?? 0,
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
            MobId = GetStringProperty(mobJson, "id") ?? Guid.NewGuid().ToString(),
            Name = GetStringProperty(mobJson, "name") ?? "Unknown Mob",
            Type = GetStringProperty(mobJson, "type") ?? string.Empty,
            Category = GetStringProperty(mobJson, "category") ?? string.Empty,
            Level = GetIntProperty(mobJson, "level"),
            Location = GetStringProperty(mobJson, "location") ?? string.Empty,
            ImageUrl = GetStringProperty(mobJson, "image") ?? GetStringProperty(mobJson, "icon") ?? string.Empty,
            IsBoss = GetBoolProperty(mobJson, "isBoss") || GetBoolProperty(mobJson, "boss"),
            IsElite = GetBoolProperty(mobJson, "isElite") || GetBoolProperty(mobJson, "elite"),
            RawJson = rawJson,
            CachedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
    }
}
