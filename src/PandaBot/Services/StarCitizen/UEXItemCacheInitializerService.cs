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
    private const string ItemsListEndpoint = "/2.0/items";
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
            _logger.LogInformation("Starting UEX item cache initialization");

            using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
            
            // Check if cache is already populated
            var existingCount = await dbContext.UexItemCache.CountAsync(cancellationToken: stoppingToken);
            if (existingCount > 0)
            {
                _logger.LogInformation("UEX item cache already populated with {Count} items", existingCount);
                return;
            }

            _logger.LogInformation("Cache is empty, fetching items from UEX API");
            
            // Fetch items from API
            var items = await FetchAllItemsFromApiAsync(stoppingToken);
            
            if (items == null || items.Count == 0)
            {
                _logger.LogWarning("No items fetched from UEX API");
                return;
            }

            _logger.LogInformation("Fetched {Count} items from UEX API, caching to database", items.Count);
            
            // Cache items in batches to avoid overwhelming the database
            var cacheEntries = items.Select(item => new ItemCache
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
                _logger.LogDebug("Cached batch {BatchNum} of {TotalBatches} ({Count} items)", 
                    (i / InitialCacheBatchSize) + 1, 
                    (cacheEntries.Count + InitialCacheBatchSize - 1) / InitialCacheBatchSize,
                    batch.Count);
            }

            _logger.LogInformation("Successfully cached {Count} items to database", cacheEntries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing UEX item cache");
            // Don't throw - allow bot to continue even if cache initialization fails
        }
    }

    /// <summary>
    /// Fetch all items from the UEX API
    /// </summary>
    private async Task<List<Item>?> FetchAllItemsFromApiAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_config.ApiBaseUrl ?? "https://api.uexcorp.uk");
            httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

            // Set up bearer token authentication if configured
            if (!string.IsNullOrWhiteSpace(_config.BearerToken))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.BearerToken}");
            }

            _logger.LogDebug("Querying UEX API for all items: {Endpoint}", ItemsListEndpoint);
            
            // Fetch all items - the API should return all items if no filters are specified
            var response = await httpClient.GetAsync(ItemsListEndpoint, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("UEX API returned status {StatusCode} when fetching items", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataArray))
            {
                _logger.LogWarning("UEX API response missing 'data' property");
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
                    _logger.LogDebug(ex, "Error parsing item from UEX API response");
                    // Continue processing other items
                }
            }

            _logger.LogInformation("Parsed {Count} valid items from UEX API response", items.Count);
            return items;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching items from UEX API");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error fetching items from UEX API");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching items from UEX API");
            return null;
        }
    }
}
