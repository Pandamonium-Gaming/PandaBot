using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PandaBot.Core.Data;
using PandaBot.Models;
using PandaBot.Models.StarCitizen;
using System.Text.Json;

namespace PandaBot.Services.StarCitizen;

/// <summary>
/// Hosted service that initializes the UEX item cache on startup by fetching all items from the API
/// </summary>
public class UEXItemCacheInitializerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UEXItemCacheInitializerService> _logger;
    private readonly UEXConfig _config;
    private const string CategoriesEndpoint = "/2.0/categories";
    private const string ItemsEndpoint = "/2.0/items";
    private const int InitialCacheBatchSize = 100; // Process items in batches

    public UEXItemCacheInitializerService(
        IServiceProvider serviceProvider,
        ILogger<UEXItemCacheInitializerService> logger,
        IOptions<UEXConfig> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("========== Starting UEX item cache initialization ==========");

            using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
            
            // Check if cache is already populated
            var existingCount = await dbContext.UexItemCache.CountAsync(cancellationToken: stoppingToken);
            _logger.LogInformation("Current cache size: {Count} items", existingCount);
            
            if (existingCount > 0)
            {
                _logger.LogInformation("UEX item cache already populated with {Count} items. Skipping initialization.", existingCount);
                return;
            }

            _logger.LogInformation("Cache is empty. Fetching categories from UEX API...");
            
            // First, fetch all categories
            var categories = await FetchCategoriesFromApiAsync(stoppingToken);
            
            if (categories == null || categories.Count == 0)
            {
                _logger.LogWarning("========== No categories found from UEX API. Cannot fetch items. ==========");
                _logger.LogWarning("Cache initialization will be skipped. Items will be cached lazily as users search.");
                return;
            }

            _logger.LogInformation("Fetched {Count} categories from UEX API", categories.Count);
            
            // Now fetch items for each category
            var allItems = new List<Item>();
            foreach (var category in categories)
            {
                _logger.LogInformation("Fetching items for category: {CategoryName} (ID: {CategoryId})", category.Name, category.Id);
                var categoryItems = await FetchItemsForCategoryAsync(category.Id, stoppingToken);
                if (categoryItems != null && categoryItems.Count > 0)
                {
                    allItems.AddRange(categoryItems);
                    _logger.LogInformation("Fetched {Count} items for category {CategoryName}", categoryItems.Count, category.Name);
                }
            }

            if (allItems.Count == 0)
            {
                _logger.LogWarning("========== No items found from UEX API. ==========");
                _logger.LogWarning("Cache initialization will be skipped. Items will be cached lazily as users search.");
                return;
            }

            _logger.LogInformation("Fetched total of {Count} items from UEX API, caching to database...", allItems.Count);
            
            // Cache items in batches to avoid overwhelming the database
            var cacheEntries = allItems.Select(item => new ItemCache
            {
                UexItemId = item.Id,
                Name = item.Name,
                Category = item.Category,
                Company = item.Company,
                CachedAt = DateTime.UtcNow
            }).ToList();

            // Add in batches
            for (int i = 0; i < cacheEntries.Count; i += InitialCacheBatchSize)
            {
                var batch = cacheEntries.Skip(i).Take(InitialCacheBatchSize).ToList();
                dbContext.UexItemCache.AddRange(batch);
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Cached batch {BatchNum} of {TotalBatches} ({Count} items)", 
                    (i / InitialCacheBatchSize) + 1, 
                    (cacheEntries.Count + InitialCacheBatchSize - 1) / InitialCacheBatchSize,
                    batch.Count);
            }

            var finalCount = await dbContext.UexItemCache.CountAsync(cancellationToken: stoppingToken);
            _logger.LogInformation("========== UEX item cache initialization complete! Total cached: {Count} items ==========", finalCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UEX item cache initialization was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "========== Error initializing UEX item cache ==========");
            _logger.LogError("Cache initialization failed. Items will be cached lazily as users search.");
            // Don't throw - allow bot to continue even if cache initialization fails
        }
    }

    /// <summary>
    /// Fetch all categories from the UEX API
    /// </summary>
    private async Task<List<Category>?> FetchCategoriesFromApiAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_config.ApiBaseUrl ?? "https://api.uexcorp.uk");
            httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(_config.BearerToken))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.BearerToken}");
            }

            var fullUrl = $"{_config.ApiBaseUrl ?? "https://api.uexcorp.uk"}{CategoriesEndpoint}";
            _logger.LogInformation("Fetching categories from UEX API: {Url}", fullUrl);
            
            var response = await httpClient.GetAsync(CategoriesEndpoint, cancellationToken);
            _logger.LogInformation("UEX API responded with status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("UEX API returned status {StatusCode} when fetching categories", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("UEX API response length: {Length} bytes", content.Length);
            
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataArray))
            {
                _logger.LogWarning("UEX API response missing 'data' property for categories");
                return null;
            }

            var categories = new List<Category>();
            foreach (var catElement in dataArray.EnumerateArray())
            {
                try
                {
                    var category = new Category
                    {
                        Id = catElement.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                        Name = catElement.TryGetProperty("name", out var name) ? name.GetString() ?? "Unknown" : "Unknown"
                    };

                    if (category.Id > 0 && !string.IsNullOrWhiteSpace(category.Name))
                    {
                        categories.Add(category);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error parsing category from UEX API response");
                }
            }

            _logger.LogInformation("Parsed {Count} valid categories from UEX API response", categories.Count);
            return categories;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching categories from UEX API");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error fetching categories from UEX API");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching categories from UEX API");
            return null;
        }
    }

    /// <summary>
    /// Fetch items for a specific category from the UEX API
    /// </summary>
    private async Task<List<Item>?> FetchItemsForCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_config.ApiBaseUrl ?? "https://api.uexcorp.uk");
            httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(_config.BearerToken))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.BearerToken}");
            }

            var queryUrl = $"{ItemsEndpoint}?id_category={categoryId}";
            _logger.LogDebug("Fetching items for category ID {CategoryId} from UEX API: {Url}", categoryId, queryUrl);
            
            var response = await httpClient.GetAsync(queryUrl, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UEX API returned status {StatusCode} when fetching items for category {CategoryId}", 
                    response.StatusCode, categoryId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataArray))
            {
                _logger.LogDebug("UEX API response missing 'data' property for category {CategoryId}", categoryId);
                return null;
            }

            var items = new List<Item>();
            foreach (var itemElement in dataArray.EnumerateArray())
            {
                try
                {
                    var item = new Item
                    {
                        Id = itemElement.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                        Name = itemElement.TryGetProperty("name", out var name) ? name.GetString() ?? "Unknown" : "Unknown",
                        Category = itemElement.TryGetProperty("category", out var cat) ? cat.GetString() ?? "Unknown" : "Unknown",
                        Company = itemElement.TryGetProperty("company_name", out var company) ? company.GetString() : null
                    };

                    if (item.Id > 0 && !string.IsNullOrWhiteSpace(item.Name))
                    {
                        items.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error parsing item from UEX API response for category {CategoryId}", categoryId);
                }
            }

            _logger.LogDebug("Parsed {Count} valid items for category {CategoryId}", items.Count, categoryId);
            return items;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching items for category {CategoryId} from UEX API", categoryId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error fetching items for category {CategoryId} from UEX API", categoryId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching items for category {CategoryId} from UEX API", categoryId);
            return null;
        }
    }
}
