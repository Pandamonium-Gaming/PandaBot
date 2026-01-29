using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PandaBot.Services.AshesOfCreation;

public class ItemCacheBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ItemCacheBackgroundService> _logger;

    public ItemCacheBackgroundService(IServiceProvider serviceProvider, ILogger<ItemCacheBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 10 seconds for bot to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        _logger.LogInformation("Starting item cache background service...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var apiService = scope.ServiceProvider.GetRequiredService<AshesForgeApiService>();
                
                _logger.LogInformation("Fetching and caching all items from API...");
                
                // Run on a background thread with lower priority to not block interactions
                await Task.Run(async () =>
                {
                    var items = await apiService.FetchAllItemsAsync();
                    
                    if (items.Count > 0)
                    {
                        _logger.LogInformation("Successfully cached {Count} items", items.Count);
                    }
                    else
                    {
                        _logger.LogWarning("No items fetched from API");
                    }
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching items from API");
            }

            // Refresh cache every 6 hours
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
