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
    private const string VehiclesPurchasePricesEndpoint = "https://api.uexcorp.uk/2.0/vehicles_purchases_prices";
    private const string VehiclesRentalPricesEndpoint = "https://api.uexcorp.uk/2.0/vehicles_rentals_prices";
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
                var lastUpdated = DateTime.UtcNow;
                if (priceElement.TryGetProperty("date_modified", out var dm) && dm.TryGetInt64(out var dmVal))
                    lastUpdated = DateTimeOffset.FromUnixTimeSeconds(dmVal).UtcDateTime;
                else if (priceElement.TryGetProperty("date_added", out var da) && da.TryGetInt64(out var daVal))
                    lastUpdated = DateTimeOffset.FromUnixTimeSeconds(daVal).UtcDateTime;

                var price = new VehiclePrice
                {
                    VehicleId = vehicleId,
                    TerminalCode = priceElement.TryGetProperty("terminal_code", out var tc) ? tc.GetString() ?? "" : "",
                    TerminalName = priceElement.TryGetProperty("terminal_name", out var tn) ? tn.GetString() ?? "" : "",
                    LocationName = priceElement.TryGetProperty("location_name", out var ln) ? ln.GetString() ?? "" : "",
                    BuyPrice = priceElement.TryGetProperty("buy_price", out var bp) && bp.TryGetDecimal(out var bpVal) ? bpVal : 0,
                    SellPrice = priceElement.TryGetProperty("sell_price", out var sp) && sp.TryGetDecimal(out var spVal) ? spVal : 0,
                    OnSale = GetBoolProperty(priceElement, "on_sale"),
                    OnSaleWarbond = GetBoolProperty(priceElement, "on_sale_warbond"),
                    OnSalePackage = GetBoolProperty(priceElement, "on_sale_package"),
                    OnSaleConcierge = GetBoolProperty(priceElement, "on_sale_concierge"),
                    Currency = priceElement.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "" : "",
                    GameVersion = priceElement.TryGetProperty("game_version", out var gv) ? gv.GetString() ?? "" : "",
                    Timestamp = priceElement.TryGetProperty("timestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var tsVal)
                        ? tsVal
                        : lastUpdated
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

            var purchasePrices = await FetchVehiclePurchasePricesAsync(vehicleId);
            var rentalPrices = await FetchVehicleRentalPricesAsync(vehicleId);

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
                var avgBuy = buyPrices.Average(p => p.BuyPrice);
                embed.AddField("💰 Cheapest Buy Price",
                    $"{cheapest.BuyPrice:F2} aUEC @ {cheapest.LocationName}\nAvg: {avgBuy:F2} aUEC",
                    inline: false);
            }

            if (sellPrices.Any())
            {
                var highest = sellPrices.OrderByDescending(p => p.SellPrice).First();
                var avgSell = sellPrices.Average(p => p.SellPrice);
                embed.AddField("📈 Best Sell Price",
                    $"{highest.SellPrice:F2} aUEC @ {highest.LocationName}\nAvg: {avgSell:F2} aUEC",
                    inline: false);
            }

            // Enrich location information
            var locations = prices.Select(p => p.LocationName).Where(l => !string.IsNullOrWhiteSpace(l)).Distinct().OrderBy(l => l).ToList();
            if (locations.Any())
            {
                var locationInfo = string.Join(", ", locations);
                if (locationInfo.Length > 1024) // Discord field value max length
                    locationInfo = string.Join(", ", locations.Take(Math.Min(5, locations.Count))) + (locations.Count > 5 ? $", +{locations.Count - 5} more" : "");

                embed.AddField("📍 Locations", $"{locations.Count} location(s): {locationInfo}", inline: false);
            }
            else
            {
                var currencies = prices.Select(p => p.Currency).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
                var currencyText = currencies.Count == 0 ? "Unknown" : string.Join(", ", currencies);
                var gameVersions = prices.Select(p => p.GameVersion).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();
                var versionText = gameVersions.Count == 0 ? "Unknown" : string.Join(", ", gameVersions);

                var availability = new List<string>
                {
                    $"On Sale: {(prices.Any(p => p.OnSale) ? "Yes" : "No")}",
                    $"Warbond: {(prices.Any(p => p.OnSaleWarbond) ? "Yes" : "No")}",
                    $"Package: {(prices.Any(p => p.OnSalePackage) ? "Yes" : "No")}",
                    $"Concierge: {(prices.Any(p => p.OnSaleConcierge) ? "Yes" : "No")}",
                    $"Currency: {currencyText}",
                    $"Game Version: {versionText}"
                };

                embed.AddField("🛒 Availability", string.Join("\n", availability), inline: false);
            }

            if (purchasePrices.Any())
            {
                var cheapestPurchase = purchasePrices.OrderBy(p => p.PriceBuy).First();
                embed.AddField("🛒 Purchase Price",
                    $"{cheapestPurchase.PriceBuy:F0} aUEC @ {FormatLocation(cheapestPurchase)}",
                    inline: false);

                var purchaseLocations = purchasePrices
                    .Select(FormatLocation)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();
                embed.AddField("🧾 Purchase Locations",
                    $"{purchaseLocations.Count} location(s): {BuildLocationList(purchaseLocations)}",
                    inline: false);
            }

            if (rentalPrices.Any())
            {
                var cheapestRental = rentalPrices.OrderBy(p => p.PriceRent).First();
                embed.AddField("🛠️ Rental Price",
                    $"{cheapestRental.PriceRent:F0} aUEC @ {FormatLocation(cheapestRental)}",
                    inline: false);

                var rentalLocations = rentalPrices
                    .Select(FormatLocation)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();
                embed.AddField("🧾 Rental Locations",
                    $"{rentalLocations.Count} location(s): {BuildLocationList(rentalLocations)}",
                    inline: false);
            }

            var timestamps = new List<DateTime>(prices.Select(p => p.Timestamp));
            timestamps.AddRange(purchasePrices.Select(p => p.Timestamp));
            timestamps.AddRange(rentalPrices.Select(p => p.Timestamp));
            var lastUpdated = timestamps.Count == 0 ? DateTime.UtcNow : timestamps.Max();
            embed.WithFooter($"Last updated: {lastUpdated:yyyy-MM-dd HH:mm} UTC");
            embed.WithTimestamp(lastUpdated);

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

    private async Task<List<VehiclePurchasePrice>> FetchVehiclePurchasePricesAsync(int vehicleId)
    {
        var cacheKey = $"uex_vehicle_purchase_{vehicleId}";
        if (_cache.TryGetValue(cacheKey, out List<VehiclePurchasePrice>? cachedPrices) && cachedPrices != null)
            return cachedPrices;

        var response = await _httpClient.GetAsync($"{VehiclesPurchasePricesEndpoint}?id_vehicle={vehicleId}");
        if (!response.IsSuccessStatusCode)
            return new();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var dataArray) || dataArray.ValueKind != JsonValueKind.Array)
            return new();

        var prices = new List<VehiclePurchasePrice>();
        foreach (var priceElement in dataArray.EnumerateArray())
        {
            var timestamp = DateTime.UtcNow;
            if (priceElement.TryGetProperty("date_modified", out var dm) && dm.TryGetInt64(out var dmVal))
                timestamp = DateTimeOffset.FromUnixTimeSeconds(dmVal).UtcDateTime;
            else if (priceElement.TryGetProperty("date_added", out var da) && da.TryGetInt64(out var daVal))
                timestamp = DateTimeOffset.FromUnixTimeSeconds(daVal).UtcDateTime;

            prices.Add(new VehiclePurchasePrice
            {
                VehicleId = vehicleId,
                PriceBuy = priceElement.TryGetProperty("price_buy", out var pb) && pb.TryGetDecimal(out var pbVal) ? pbVal : 0,
                StarSystemName = priceElement.TryGetProperty("star_system_name", out var ss) ? ss.GetString() ?? "" : "",
                PlanetName = priceElement.TryGetProperty("planet_name", out var pn) ? pn.GetString() ?? "" : "",
                OrbitName = priceElement.TryGetProperty("orbit_name", out var on) ? on.GetString() ?? "" : "",
                MoonName = priceElement.TryGetProperty("moon_name", out var mn) ? mn.GetString() ?? "" : "",
                SpaceStationName = priceElement.TryGetProperty("space_station_name", out var sn) ? sn.GetString() ?? "" : "",
                CityName = priceElement.TryGetProperty("city_name", out var cn) ? cn.GetString() ?? "" : "",
                OutpostName = priceElement.TryGetProperty("outpost_name", out var op) ? op.GetString() ?? "" : "",
                PoiName = priceElement.TryGetProperty("poi_name", out var poi) ? poi.GetString() ?? "" : "",
                TerminalName = priceElement.TryGetProperty("terminal_name", out var tn) ? tn.GetString() ?? "" : "",
                TerminalCode = priceElement.TryGetProperty("terminal_code", out var tc) ? tc.GetString() ?? "" : "",
                GameVersion = priceElement.TryGetProperty("game_version", out var gv) ? gv.GetString() ?? "" : "",
                Timestamp = timestamp
            });
        }

        _cache.Set(cacheKey, prices, TimeSpan.FromMinutes(ItemCacheDurationMinutes));
        return prices;
    }

    private async Task<List<VehicleRentalPrice>> FetchVehicleRentalPricesAsync(int vehicleId)
    {
        var cacheKey = $"uex_vehicle_rental_{vehicleId}";
        if (_cache.TryGetValue(cacheKey, out List<VehicleRentalPrice>? cachedPrices) && cachedPrices != null)
            return cachedPrices;

        var response = await _httpClient.GetAsync($"{VehiclesRentalPricesEndpoint}?id_vehicle={vehicleId}");
        if (!response.IsSuccessStatusCode)
            return new();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var dataArray) || dataArray.ValueKind != JsonValueKind.Array)
            return new();

        var prices = new List<VehicleRentalPrice>();
        foreach (var priceElement in dataArray.EnumerateArray())
        {
            var timestamp = DateTime.UtcNow;
            if (priceElement.TryGetProperty("date_modified", out var dm) && dm.TryGetInt64(out var dmVal))
                timestamp = DateTimeOffset.FromUnixTimeSeconds(dmVal).UtcDateTime;
            else if (priceElement.TryGetProperty("date_added", out var da) && da.TryGetInt64(out var daVal))
                timestamp = DateTimeOffset.FromUnixTimeSeconds(daVal).UtcDateTime;

            prices.Add(new VehicleRentalPrice
            {
                VehicleId = vehicleId,
                PriceRent = priceElement.TryGetProperty("price_rent", out var pr) && pr.TryGetDecimal(out var prVal) ? prVal : 0,
                StarSystemName = priceElement.TryGetProperty("star_system_name", out var ss) ? ss.GetString() ?? "" : "",
                PlanetName = priceElement.TryGetProperty("planet_name", out var pn) ? pn.GetString() ?? "" : "",
                OrbitName = priceElement.TryGetProperty("orbit_name", out var on) ? on.GetString() ?? "" : "",
                MoonName = priceElement.TryGetProperty("moon_name", out var mn) ? mn.GetString() ?? "" : "",
                SpaceStationName = priceElement.TryGetProperty("space_station_name", out var sn) ? sn.GetString() ?? "" : "",
                CityName = priceElement.TryGetProperty("city_name", out var cn) ? cn.GetString() ?? "" : "",
                OutpostName = priceElement.TryGetProperty("outpost_name", out var op) ? op.GetString() ?? "" : "",
                PoiName = priceElement.TryGetProperty("poi_name", out var poi) ? poi.GetString() ?? "" : "",
                TerminalName = priceElement.TryGetProperty("terminal_name", out var tn) ? tn.GetString() ?? "" : "",
                TerminalCode = priceElement.TryGetProperty("terminal_code", out var tc) ? tc.GetString() ?? "" : "",
                GameVersion = priceElement.TryGetProperty("game_version", out var gv) ? gv.GetString() ?? "" : "",
                Timestamp = timestamp
            });
        }

        _cache.Set(cacheKey, prices, TimeSpan.FromMinutes(ItemCacheDurationMinutes));
        return prices;
    }

    private static string FormatLocation(VehiclePurchasePrice price)
    {
        return FormatLocationInternal(price.TerminalName, price.CityName, price.PlanetName, price.StarSystemName);
    }

    private static string FormatLocation(VehicleRentalPrice price)
    {
        return FormatLocationInternal(price.TerminalName, price.CityName, price.PlanetName, price.StarSystemName);
    }

    private static string FormatLocationInternal(string terminalName, string cityName, string planetName, string starSystemName)
    {
        var system = starSystemName?.Trim() ?? string.Empty;
        var planet = planetName?.Trim() ?? string.Empty;
        var location = !string.IsNullOrWhiteSpace(terminalName)
            ? terminalName.Trim()
            : !string.IsNullOrWhiteSpace(cityName)
                ? cityName.Trim()
                : string.Empty;

        if (string.IsNullOrWhiteSpace(location) && !string.IsNullOrWhiteSpace(planet))
            location = planet;

        if (string.IsNullOrWhiteSpace(system) && string.IsNullOrWhiteSpace(planet) && string.IsNullOrWhiteSpace(location))
            return "Unknown";

        if (string.IsNullOrWhiteSpace(system))
            return string.IsNullOrWhiteSpace(planet) ? location : $"{planet} > {location}";

        if (string.IsNullOrWhiteSpace(planet))
            return string.IsNullOrWhiteSpace(location) ? system : $"{system} > {location}";

        if (string.IsNullOrWhiteSpace(location))
            return $"{system} > {planet}";

        return $"{system} > {planet} > {location}";
    }

    private static string BuildLocationList(List<string> locations)
    {
        if (locations.Count == 0)
            return "None";

        var locationInfo = string.Join(", ", locations);
        if (locationInfo.Length > 1024)
            locationInfo = string.Join(", ", locations.Take(Math.Min(5, locations.Count))) + (locations.Count > 5 ? $", +{locations.Count - 5} more" : "");
        return locationInfo;
    }

    private static bool GetBoolProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.True)
                return true;
            if (prop.ValueKind == JsonValueKind.False)
                return false;
            if (prop.TryGetInt32(out var intValue))
                return intValue != 0;
            if (prop.ValueKind == JsonValueKind.String)
            {
                var strValue = prop.GetString()?.ToLower() ?? "";
                return strValue is "1" or "true" or "yes";
            }
        }
        return false;
    }
}
