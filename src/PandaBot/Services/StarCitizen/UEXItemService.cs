using Discord;
using PandaBot.Models;
using PandaBot.Models.StarCitizen;
using PandaBot.Core.Data;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace PandaBot.Services.StarCitizen;

/// <summary>
/// Service for fetching item pricing data from UEX Corp API
/// </summary>
public class UEXItemService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UEXItemService> _logger;
    private readonly UEXConfig _config;
    private readonly IMemoryCache _cache;
    private readonly PandaBotContext _dbContext;
    private const string CategoriesEndpoint = "/2.0/categories";
    private const string ItemsEndpoint = "/2.0/items";
    private const string ItemsPricesEndpoint = "/2.0/items_prices";
    private const string UexBadgeUrl = "https://uexcorp.space/img/api/uex-api-badge-powered.png";
    private const int ItemCacheDurationMinutes = 24 * 60; // 24 hours to match API cache
    private const int FuzzySearchTopResults = 5; // Return top 5 fuzzy matches

    public UEXItemService(HttpClient httpClient, ILogger<UEXItemService> logger, IOptions<UEXConfig> config, IMemoryCache cache, PandaBotContext dbContext)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
        _cache = cache;
        _dbContext = dbContext;

        // Configure HttpClient with base address and timeout
        _httpClient.BaseAddress = new Uri(_config.ApiBaseUrl ?? "https://api.uexcorp.uk");
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        // Set up bearer token authentication if configured
        if (!string.IsNullOrWhiteSpace(_config.BearerToken))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.BearerToken}");
        }
    }

    /// <summary>
    /// Search for items by name with fuzzy matching, returning top matches
    /// </summary>
    public async Task<List<ItemCache>> SearchItemsByNameFuzzyAsync(string searchTerm, int maxResults = FuzzySearchTopResults)
    {
        try
        {
            _logger.LogInformation("Searching UEX items by name: {SearchTerm}", searchTerm);

            // Calculate the expiration cutoff (24 hours ago)
            var expirationCutoff = DateTime.UtcNow.AddHours(-24);

            // Get non-expired cached items from database
            var cachedItems = await _dbContext.UexItemCache
                .Where(x => x.CachedAt > expirationCutoff)
                .OrderBy(x => x.Name)
                .ToListAsync();

            if (!cachedItems.Any())
            {
                _logger.LogWarning("No items found in cache, attempting to refresh from API");
                var refreshSuccess = await RefreshItemCacheAsync();
                if (!refreshSuccess)
                {
                    _logger.LogError("Failed to refresh item cache from API");
                    return new List<ItemCache>();
                }
                
                // Retry the query after refresh
                cachedItems = await _dbContext.UexItemCache
                    .Where(x => x.CachedAt > expirationCutoff)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                
                if (!cachedItems.Any())
                {
                    _logger.LogWarning("No items found even after cache refresh");
                    return new List<ItemCache>();
                }
            }

            _logger.LogDebug("Found {Count} non-expired cached items for fuzzy search", cachedItems.Count);

            // Score and sort results by similarity
            var scoredResults = cachedItems
                .Select(item => new
                {
                    Item = item,
                    Score = ItemCache.SimilarityScore(searchTerm, item.Name)
                })
                .Where(x => x.Score > 0) // Only return items with at least some similarity
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => x.Item)
                .ToList();

            _logger.LogInformation("Found {Count} fuzzy matches for: {SearchTerm}", scoredResults.Count, searchTerm);
            return scoredResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing fuzzy search for items: {SearchTerm}", searchTerm);
            return new List<ItemCache>();
        }
    }

    /// <summary>
    /// Get a single item by exact UEX item ID
    /// </summary>
    public async Task<ItemCache?> GetCachedItemByIdAsync(int uexItemId)
    {
        try
        {
            // Calculate the expiration cutoff (24 hours ago)
            var expirationCutoff = DateTime.UtcNow.AddHours(-24);

            var cached = await _dbContext.UexItemCache
                .FirstOrDefaultAsync(x => x.UexItemId == uexItemId && x.CachedAt > expirationCutoff);
            return cached;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached item by ID: {UexItemId}", uexItemId);
            return null;
        }
    }

    /// <summary>
    /// Search for an item by name and return formatted pricing information for first match
    /// </summary>
    public async Task<Embed?> GetItemPricesEmbedAsync(int itemId)
    {
        try
        {
            _logger.LogInformation("Fetching UEX prices for item ID: {ItemId}", itemId);

            var cachedItem = await GetCachedItemByIdAsync(itemId);
            if (cachedItem == null)
            {
                _logger.LogWarning("Item not found in cache: {ItemId}", itemId);
                return null;
            }

            var prices = await FetchItemPricesAsync(itemId);
            if (prices == null || prices.Count == 0)
            {
                _logger.LogWarning("No price data found for item: {ItemId}", itemId);
                return null;
            }

            // Build item object with prices
            var item = new Item
            {
                Id = cachedItem.UexItemId,
                Name = cachedItem.Name,
                Category = cachedItem.Category,
                Company = cachedItem.Company,
                Prices = prices
            };

            // Calculate summary
            var summary = CalculateSummary(item);

            // Build embed
            var embed = new EmbedBuilder()
                .WithTitle($"🛠️ {item.Name} - Price Summary")
                .WithColor(Color.Blue)
                .WithDescription($"**Category:** {item.Category}")
                .WithThumbnailUrl(UexBadgeUrl);

            if (!string.IsNullOrWhiteSpace(item.Company))
            {
                embed.AddField("🏢 Manufacturer", item.Company, inline: true);
            }

            // Add price summary fields
            embed.AddField("💰 Lowest Price", 
                $"{summary.LowestPrice:F2} aUEC @ {summary.CheapestLocation}", 
                inline: false);
            
            embed.AddField("📈 Highest Price", 
                $"{summary.HighestPrice:F2} aUEC @ {summary.MostExpensiveLocation}", 
                inline: false);

            embed.AddField("📍 Locations", 
                $"{summary.LocationCount} locations tracked", 
                inline: true);

            if (summary.LowestPrice > 0)
            {
                embed.AddField("💹 Price Spread", 
                    $"{((summary.HighestPrice - summary.LowestPrice) / summary.LowestPrice * 100):F1}%", 
                    inline: true);
            }

            // Add top 5 cheapest locations
            var cheapest = item.Prices
                .Where(p => p.BuyPrice > 0)
                .OrderBy(p => p.BuyPrice)
                .Take(5)
                .ToList();

            if (cheapest.Any())
            {
                var cheapestText = string.Join("\n", 
                    cheapest.Select(p => $"• {p.LocationName} ({p.TerminalCode}): {p.BuyPrice:F2} aUEC"));
                embed.AddField("✅ Cheapest 5 Locations", cheapestText, inline: false);
            }

            // Add top 5 most expensive locations
            var expensive = item.Prices
                .Where(p => p.SellPrice > 0)
                .OrderByDescending(p => p.SellPrice)
                .Take(5)
                .ToList();

            if (expensive.Any())
            {
                var expensiveText = string.Join("\n", 
                    expensive.Select(p => $"• {p.LocationName} ({p.TerminalCode}): {p.SellPrice:F2} aUEC"));
                embed.AddField("🔴 Most Expensive 5 Locations", expensiveText, inline: false);
            }

            embed.WithFooter($"Last updated: {summary.LastUpdated:yyyy-MM-dd HH:mm} UTC");
            embed.WithTimestamp(DateTime.UtcNow);

            return embed.Build();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching UEX pricing data for item ID: {ItemId}", itemId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for UEX item ID: {ItemId}", itemId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching UEX pricing for item ID: {ItemId}", itemId);
            return null;
        }
    }

    /// <summary>
    /// Fetch item by UUID with caching (memory first, then database, then API)
    /// </summary>
    private async Task<Item?> FetchItemAsync(string itemName)
    {
        try
        {
            // Check memory cache first
            var cacheKey = $"uex_item_{itemName.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out Item? cachedItem))
            {
                _logger.LogDebug("Item found in memory cache: {ItemName}", itemName);
                return cachedItem;
            }

            // Try searching by UUID
            var url = $"{ItemsEndpoint}?uuid={Uri.EscapeDataString(itemName)}";
            _logger.LogDebug("Querying UEX API for item: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var item = ParseItemResponse(content, itemName);
                if (item != null)
                {
                    // Cache the result in memory
                    _cache.Set(cacheKey, item, TimeSpan.FromMinutes(ItemCacheDurationMinutes));
                    _logger.LogDebug("Item cached in memory for {ItemName}", itemName);
                    
                    // Also cache in database
                    await CacheItemAsync(item);
                    
                    return item;
                }
            }

            _logger.LogWarning("Item not found in UEX API: {ItemName}", itemName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching item from UEX API: {ItemName}", itemName);
            return null;
        }
    }

    /// <summary>
    /// Cache an item to the database
    /// </summary>
    private async Task CacheItemAsync(Item item)
    {
        try
        {
            // Check if item already exists in cache
            var existingCache = await _dbContext.UexItemCache
                .FirstOrDefaultAsync(x => x.UexItemId == item.Id);

            if (existingCache != null)
            {
                // Update existing cache entry
                existingCache.Name = item.Name;
                existingCache.Category = item.Category;
                existingCache.Company = item.Company;
                existingCache.CachedAt = DateTime.UtcNow;
                _dbContext.UexItemCache.Update(existingCache);
            }
            else
            {
                // Create new cache entry
                var cacheEntry = new ItemCache
                {
                    UexItemId = item.Id,
                    Name = item.Name,
                    Category = item.Category,
                    Company = item.Company,
                    CachedAt = DateTime.UtcNow
                };
                _dbContext.UexItemCache.Add(cacheEntry);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogDebug("Item cached to database: {ItemName} (ID: {ItemId})", item.Name, item.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching item to database: {ItemName}", item.Name);
            // Don't throw - caching to database is optional
        }
    }

    /// <summary>
    /// Fetch prices for a specific item with caching
    /// </summary>
    private async Task<List<ItemPrice>?> FetchItemPricesAsync(int itemId)
    {
        try
        {
            // Check cache first
            var cacheKey = $"uex_item_prices_{itemId}";
            if (_cache.TryGetValue(cacheKey, out List<ItemPrice>? cachedPrices))
            {
                _logger.LogDebug("Item prices found in cache: {ItemId}", itemId);
                return cachedPrices;
            }

            var url = $"{ItemsPricesEndpoint}?id_item={itemId}";
            _logger.LogDebug("Querying UEX API for item prices: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UEX API returned status {StatusCode} for item ID: {ItemId}", 
                    response.StatusCode, itemId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Parse the price data from the response
            if (!root.TryGetProperty("data", out var dataArray) || dataArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("UEX API returned no price data for item ID: {ItemId}", itemId);
                return null;
            }

            var prices = new List<ItemPrice>();
            foreach (var priceElement in dataArray.EnumerateArray())
            {
                var price = new ItemPrice
                {
                    Id = priceElement.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                    ItemId = priceElement.TryGetProperty("id_item", out var itemIdProp) ? itemIdProp.GetInt32() : itemId,
                    TerminalCode = priceElement.TryGetProperty("terminal_code", out var termCode) ? termCode.GetString() ?? "" : "",
                    TerminalName = priceElement.TryGetProperty("terminal_name", out var termName) ? termName.GetString() ?? "" : "",
                    LocationName = priceElement.TryGetProperty("planet", out var planet) ? planet.GetString() ?? "" : "",
                    BuyPrice = priceElement.TryGetProperty("price_buy", out var buy) ? (decimal)buy.GetDouble() : 0,
                    SellPrice = priceElement.TryGetProperty("price_sell", out var sell) ? (decimal)sell.GetDouble() : 0,
                    Timestamp = priceElement.TryGetProperty("time_update", out var ts) ? ts.GetString() ?? "" : ""
                };
                prices.Add(price);
            }

            // Cache the results
            _cache.Set(cacheKey, prices, TimeSpan.FromMinutes(ItemCacheDurationMinutes));
            _logger.LogDebug("Item prices cached for ID: {ItemId}", itemId);

            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching item prices from UEX API for item ID: {ItemId}", itemId);
            return null;
        }
    }

    /// <summary>
    /// Parse item data from API response
    /// </summary>
    private static Item? ParseItemResponse(string jsonContent, string searchTerm)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataArray) || dataArray.GetArrayLength() == 0)
            {
                return null;
            }

            var itemElement = dataArray[0];
            return new Item
            {
                Id = itemElement.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                Name = itemElement.TryGetProperty("name", out var name) ? name.GetString() ?? searchTerm : searchTerm,
                Category = itemElement.TryGetProperty("category", out var cat) ? cat.GetString() ?? "Unknown" : "Unknown",
                Company = itemElement.TryGetProperty("company_name", out var company) ? company.GetString() : null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Calculate item price summary statistics
    /// </summary>
    private static ItemSummary CalculateSummary(Item item)
    {
        var validPrices = item.Prices.Where(p => p.BuyPrice > 0 || p.SellPrice > 0).ToList();

        if (!validPrices.Any())
        {
            return new ItemSummary
            {
                ItemName = item.Name,
                Category = item.Category,
                LocationCount = 0,
                LastUpdated = DateTime.UtcNow
            };
        }

        var minPrice = validPrices.Min(p => p.BuyPrice > 0 ? p.BuyPrice : p.SellPrice);
        var maxPrice = validPrices.Max(p => p.SellPrice > 0 ? p.SellPrice : p.BuyPrice);

        var cheapest = validPrices.FirstOrDefault(p => (p.BuyPrice > 0 ? p.BuyPrice : p.SellPrice) == minPrice);
        var expensive = validPrices.FirstOrDefault(p => (p.SellPrice > 0 ? p.SellPrice : p.BuyPrice) == maxPrice);

        return new ItemSummary
        {
            ItemName = item.Name,
            Category = item.Category,
            LowestPrice = minPrice,
            HighestPrice = maxPrice,
            CheapestLocation = cheapest?.LocationName ?? "Unknown",
            MostExpensiveLocation = expensive?.LocationName ?? "Unknown",
            LocationCount = validPrices.Select(p => p.LocationName).Distinct().Count(),
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Refresh item cache by fetching all items from UEX API
    /// </summary>
    public async Task<bool> RefreshItemCacheAsync()
    {
        try
        {
            _logger.LogInformation("Starting item cache refresh from UEX API");
            var startTime = DateTime.UtcNow;

            // UEX /items endpoint now requires filters; pull categories first and query per category.
            var categoriesResponse = await _httpClient.GetAsync(CategoriesEndpoint);
            if (!categoriesResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch categories from UEX API: {StatusCode}", categoriesResponse.StatusCode);
                return false;
            }

            var categoriesContent = await categoriesResponse.Content.ReadAsStringAsync();
            using var categoriesDoc = JsonDocument.Parse(categoriesContent);
            var categoriesRoot = categoriesDoc.RootElement;

            if (!categoriesRoot.TryGetProperty("data", out var categoriesArray) || categoriesArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogError("Invalid categories response format from UEX API: missing or invalid 'data' array");
                return false;
            }

            var itemCount = 0;
            var updatedCount = 0;
            var createdCount = 0;

            foreach (var categoryElement in categoriesArray.EnumerateArray())
            {
                var categoryId = categoryElement.TryGetProperty("id", out var idValue) && idValue.TryGetInt32(out var parsedCategoryId)
                    ? parsedCategoryId
                    : 0;
                if (categoryId <= 0)
                {
                    continue;
                }

                var itemsUrl = $"{ItemsEndpoint}?id_category={categoryId}";
                var itemsResponse = await _httpClient.GetAsync(itemsUrl);
                if (!itemsResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch items for category {CategoryId} from UEX API: {StatusCode}", categoryId, itemsResponse.StatusCode);
                    continue;
                }

                var itemsContent = await itemsResponse.Content.ReadAsStringAsync();
                using var itemsDoc = JsonDocument.Parse(itemsContent);
                var itemsRoot = itemsDoc.RootElement;
                if (!itemsRoot.TryGetProperty("data", out var itemsArray) || itemsArray.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Invalid items response format from UEX API for category {CategoryId}", categoryId);
                    continue;
                }

                foreach (var itemElement in itemsArray.EnumerateArray())
                {
                    try
                    {
                        var itemId = itemElement.TryGetProperty("id", out var id) && id.TryGetInt32(out var idVal) ? idVal : 0;
                        if (itemId == 0)
                            continue;

                        var name = itemElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        var category = itemElement.TryGetProperty("category", out var c) ? c.GetString() ?? "" : "";
                        var company = itemElement.TryGetProperty("company_name", out var comp) ? comp.GetString() : null;

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        var existingCache = await _dbContext.UexItemCache
                            .FirstOrDefaultAsync(i => i.UexItemId == itemId);

                        if (existingCache != null)
                        {
                            existingCache.Name = name;
                            existingCache.Category = category;
                            existingCache.Company = company;
                            existingCache.CachedAt = DateTime.UtcNow;
                            _dbContext.UexItemCache.Update(existingCache);
                            updatedCount++;
                        }
                        else
                        {
                            var cacheEntry = new ItemCache
                            {
                                UexItemId = itemId,
                                Name = name,
                                Category = category,
                                Company = company,
                                CachedAt = DateTime.UtcNow
                            };
                            _dbContext.UexItemCache.Add(cacheEntry);
                            createdCount++;
                        }

                        itemCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing item from UEX API response for category {CategoryId}", categoryId);
                    }
                }
            }

            if (itemCount > 0)
            {
                await _dbContext.SaveChangesAsync();
                var elapsed = DateTime.UtcNow - startTime;
                _logger.LogInformation("Item cache refresh completed: {TotalCount} items processed ({CreatedCount} created, {UpdatedCount} updated) in {ElapsedSeconds:F2}s",
                    itemCount, createdCount, updatedCount, elapsed.TotalSeconds);
                return true;
            }
            else
            {
                _logger.LogWarning("No items found in API response");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing item cache from UEX API");
            return false;
        }
    }
}
