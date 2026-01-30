using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PandaBot.Services.AshesOfCreation;

public class AshesForgeDataCacheService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AshesForgeDataCacheService> _logger;

    public AshesForgeDataCacheService(IServiceProvider serviceProvider, ILogger<AshesForgeDataCacheService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 10 seconds for bot to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        _logger.LogWarning("=== Starting AshesForge data cache service ===");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var apiService = scope.ServiceProvider.GetRequiredService<AshesForgeApiService>();
                
                // Cache items
                _logger.LogWarning("Fetching and caching items from API...");
                var items = await apiService.FetchAllItemsAsync();
                
                if (items.Count > 0)
                {
                    _logger.LogWarning("Successfully cached {Count} items", items.Count);
                }
                else
                {
                    _logger.LogWarning("No items fetched from API");
                }

                // Cache recipes
                _logger.LogWarning("Fetching and caching recipes from API...");
                var recipes = await apiService.FetchAllRecipesAsync();
                
                if (recipes.Count > 0)
                {
                    _logger.LogWarning("Successfully cached {Count} recipes", recipes.Count);
                }
                else
                {
                    _logger.LogWarning("No recipes fetched from API");
                }

                // Enrich recipes with ingredient data from item details
                _logger.LogWarning("=== Starting enrichment of recipes with ingredient data ===");
                await apiService.EnrichRecipesWithIngredientsAsync();
                _logger.LogWarning("=== Enrichment process completed ===");

                // TODO: Mobs endpoint doesn't exist yet on AshesForge API
                // Uncomment this when the endpoint becomes available
                /*
                _logger.LogInformation("Fetching and caching mobs from API...");
                await Task.Run(async () =>
                {
                    var mobs = await apiService.FetchAllMobsAsync();
                    
                    if (mobs.Count > 0)
                    {
                        _logger.LogInformation("Successfully cached {Count} mobs", mobs.Count);
                    }
                    else
                    {
                        _logger.LogWarning("No mobs fetched from API");
                    }
                }, stoppingToken);
                */

                _logger.LogInformation("Data cache refresh completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching data from API");
            }

            // Refresh cache every 6 hours
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
