using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Models.StarCitizen;
using System.Globalization;
using System.Text.Json;

namespace PandaBot.Services.StarCitizen;

public class UEXVehicleService
{
    private const string VehiclesEndpoint = "https://api.uexcorp.uk/2.0/vehicles";
    private const string VehiclesPricesEndpoint = "https://api.uexcorp.uk/2.0/vehicles_prices";
    private const int ItemCacheDurationMinutes = 1440; // 24 hours
    
    private readonly HttpClient _httpClient;
    private readonly PandaBotContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UEXVehicleService> _logger;

    public UEXVehicleService(HttpClient httpClient, PandaBotContext dbContext, IMemoryCache cache, ILogger<UEXVehicleService> logger)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Search vehicles by name with fuzzy matching
    /// </summary>
    public async Task<List<VehicleCache>> SearchVehiclesByNameFuzzyAsync(string searchTerm, int maxResults = 10)
    {
        try
        {
            _logger.LogInformation("Searching UEX vehicles by name: {SearchTerm}", searchTerm);
            
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            var vehicles = await _dbContext.UexVehicleCache
                .Where(v => v.CachedAt > cutoffTime)
                .OrderBy(v => v.Name)
                .ToListAsync();

            if (!vehicles.Any())
            {
                _logger.LogWarning("No vehicles found in cache");
                return new();
            }

            // Score by similarity
            var scored = vehicles
                .Select(v => new { Vehicle = v, Score = CalculateSimilarity(searchTerm, v.Name) })
                .Where(s => s.Score > 0)
                .OrderByDescending(s => s.Score)
                .Take(maxResults)
                .Select(s => s.Vehicle)
                .ToList();

            _logger.LogInformation("Found {Count} fuzzy matches for: {SearchTerm}", scored.Count, searchTerm);
            return scored;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vehicles by name: {SearchTerm}", searchTerm);
            return new();
        }
    }

    /// <summary>
    /// Get cached vehicle by ID
    /// </summary>
    public async Task<VehicleCache?> GetCachedVehicleByIdAsync(int uexVehicleId)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            return await _dbContext.UexVehicleCache
                .Where(v => v.UexVehicleId == uexVehicleId && v.CachedAt > cutoffTime)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached vehicle: {VehicleId}", uexVehicleId);
            return null;
        }
    }

    /// <summary>
    /// Cache vehicle to database
    /// </summary>
    public async Task CacheVehicleAsync(Vehicle vehicle)
    {
        try
        {
            var existingCache = await _dbContext.UexVehicleCache
                .FirstOrDefaultAsync(v => v.UexVehicleId == vehicle.Id);

            if (existingCache != null)
            {
                existingCache.Name = vehicle.Name;
                existingCache.Type = vehicle.Type;
                existingCache.Manufacturer = vehicle.Manufacturer;
                existingCache.CachedAt = DateTime.UtcNow;
                _dbContext.UexVehicleCache.Update(existingCache);
            }
            else
            {
                var cacheEntry = new VehicleCache
                {
                    UexVehicleId = vehicle.Id,
                    Name = vehicle.Name,
                    Type = vehicle.Type,
                    Manufacturer = vehicle.Manufacturer,
                    CachedAt = DateTime.UtcNow
                };
                _dbContext.UexVehicleCache.Add(cacheEntry);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogDebug("Vehicle cached to database: {VehicleName} (ID: {VehicleId})", vehicle.Name, vehicle.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching vehicle: {VehicleName}", vehicle.Name);
        }
    }

    /// <summary>
    /// Fetch prices for a vehicle
    /// </summary>
    private async Task<List<VehiclePrice>?> FetchVehiclePricesAsync(int vehicleId)
    {
        try
        {
            _logger.LogDebug("Fetching prices for vehicle ID: {VehicleId}", vehicleId);
            var url = $"{VehiclesPricesEndpoint}?id_vehicle={vehicleId}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataArray) || dataArray.ValueKind != System.Text.Json.JsonValueKind.Array)
                return new();

            var prices = new List<VehiclePrice>();
            foreach (var priceElement in dataArray.EnumerateArray())
            {
                var price = new VehiclePrice
                {
                    VehicleId = vehicleId,
                    TerminalCode = priceElement.TryGetProperty("terminal_code", out var tc) ? tc.GetString() ?? "" : "",
                    TerminalName = priceElement.TryGetProperty("terminal_name", out var tn) ? tn.GetString() ?? "" : "",
                    LocationName = priceElement.TryGetProperty("location_name", out var ln) ? ln.GetString() ?? "" : "",
                    BuyPrice = priceElement.TryGetProperty("buy_price", out var bp) && bp.TryGetDecimal(out var bpVal) ? bpVal : 0,
                    SellPrice = priceElement.TryGetProperty("sell_price", out var sp) && sp.TryGetDecimal(out var spVal) ? spVal : 0,
                    Timestamp = priceElement.TryGetProperty("timestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var tsVal) ? tsVal : DateTime.UtcNow
                };
                prices.Add(price);
            }

            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prices for vehicle ID: {VehicleId}", vehicleId);
            return null;
        }
    }

    /// <summary>
    /// Build Discord embed with vehicle pricing information
    /// </summary>
    public async Task<Discord.Embed?> GetVehiclePricesEmbedAsync(int vehicleId)
    {
        try
        {
            _logger.LogInformation("Building embed for vehicle ID: {VehicleId}", vehicleId);

            var cachedVehicle = await GetCachedVehicleByIdAsync(vehicleId);
            if (cachedVehicle == null)
            {
                _logger.LogWarning("Vehicle not in cache: {VehicleId}", vehicleId);
                return null;
            }

            var prices = await FetchVehiclePricesAsync(vehicleId);
            if (prices == null || prices.Count == 0)
            {
                _logger.LogWarning("No prices found for vehicle: {VehicleName}", cachedVehicle.Name);
                return null;
            }

            // Build summary
            var buyPrices = prices.Where(p => p.BuyPrice > 0).ToList();
            var sellPrices = prices.Where(p => p.SellPrice > 0).ToList();

            var embed = new Discord.EmbedBuilder()
                .WithTitle($"🚀 {cachedVehicle.Name} - Price Summary")
                .WithColor(Discord.Color.Blue)
                .WithDescription($"**Type:** {cachedVehicle.Type}")
                .WithThumbnailUrl("https://uexcorp.space/img/logo_dark_64x64.png");

            if (!string.IsNullOrEmpty(cachedVehicle.Manufacturer))
                embed.AddField("🏭 Manufacturer", cachedVehicle.Manufacturer, inline: true);

            if (buyPrices.Any())
            {
                var cheapest = buyPrices.OrderBy(p => p.BuyPrice).First();
                embed.AddField("💰 Cheapest Buy Price",
                    $"{cheapest.BuyPrice:F2} aUEC @ {cheapest.LocationName}",
                    inline: false);
            }

            if (sellPrices.Any())
            {
                var highest = sellPrices.OrderByDescending(p => p.SellPrice).First();
                embed.AddField("📈 Best Sell Price",
                    $"{highest.SellPrice:F2} aUEC @ {highest.LocationName}",
                    inline: false);
            }

            embed.AddField("📍 Locations", $"{prices.Select(p => p.LocationName).Distinct().Count()} locations tracked", inline: true);
            embed.WithFooter($"Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            embed.WithTimestamp(DateTime.UtcNow);

            return embed.Build();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building vehicle prices embed for ID: {VehicleId}", vehicleId);
            return null;
        }
    }

    /// <summary>
    /// Calculate similarity score between search term and vehicle name
    /// </summary>
    private static int CalculateSimilarity(string searchTerm, string vehicleName)
    {
        var search = searchTerm.ToLower();
        var name = vehicleName.ToLower();

        if (search == name) return 100;
        if (name.Contains(search)) return 90;
        if (name.StartsWith(search)) return 80;

        // Check word-based matches
        var searchWords = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var nameWords = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var matchedWords = searchWords.Count(sw => nameWords.Any(nw => nw.Contains(sw) || sw.Contains(nw)));
        if (matchedWords > 0)
        {
            return (matchedWords * 70) / searchWords.Length;
        }

        // Levenshtein distance
        var distance = LevenshteinDistance(search, name);
        var maxLen = Math.Max(search.Length, name.Length);
        return Math.Max(0, 100 - (distance * 100 / maxLen));
    }

    /// <summary>
    /// Calculate Levenshtein distance between two strings
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;
        var d = new int[len1 + 1, len2 + 1];

        for (var i = 0; i <= len1; i++)
            d[i, 0] = i;

        for (var j = 0; j <= len2; j++)
            d[0, j] = j;

        for (var i = 1; i <= len1; i++)
        {
            for (var j = 1; j <= len2; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[len1, len2];
    }
}
