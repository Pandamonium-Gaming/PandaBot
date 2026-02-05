using Discord;
using PandaBot.Models;
using PandaBot.Models.StarCitizen;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PandaBot.Services.StarCitizen;

/// <summary>
/// Service for fetching commodity pricing data from UEX Corp API
/// </summary>
public class UEXCommodityService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UEXCommodityService> _logger;
    private readonly UEXConfig _config;
    private const string CommoditySearchEndpoint = "/2.0/commodities_prices";
    private const string UexBadgeUrl = "https://uexcorp.space/img/api/uex-api-badge-powered.png";

    public UEXCommodityService(HttpClient httpClient, ILogger<UEXCommodityService> logger, IOptions<UEXConfig> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;

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
    /// Search for a commodity by name and return formatted pricing information
    /// </summary>
    public async Task<Embed?> GetCommodityPricesEmbedAsync(string commodityName)
    {
        try
        {
            _logger.LogInformation("Fetching UEX commodity data for: {CommodityName}", commodityName);

            var commodity = await FetchCommodityAsync(commodityName);
            if (commodity == null || commodity.Prices.Count == 0)
            {
                _logger.LogWarning("No commodity data found for: {CommodityName}", commodityName);
                return null;
            }

            // Calculate summary
            var summary = CalculateSummary(commodity);

            // Build embed
            var embed = new EmbedBuilder()
                .WithTitle($"📊 {commodity.Name} - Price Summary")
                .WithColor(Color.Gold)
                .WithDescription($"**Type:** {commodity.Type}")
                .WithThumbnailUrl(UexBadgeUrl);

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

            embed.AddField("💹 Price Spread", 
                $"{((summary.HighestPrice - summary.LowestPrice) / summary.LowestPrice * 100):F1}%", 
                inline: true);

            // Add top 5 cheapest locations
            var cheapest = commodity.Prices
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
            var expensive = commodity.Prices
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
            _logger.LogError(ex, "HTTP error fetching UEX commodity data for: {CommodityName}", commodityName);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for UEX commodity: {CommodityName}", commodityName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching UEX commodity: {CommodityName}", commodityName);
            return null;
        }
    }

    /// <summary>
    /// Fetch and parse commodity data from UEX API
    /// </summary>
    private async Task<Commodity?> FetchCommodityAsync(string commodityName)
    {
        try
        {
            var url = $"{CommoditySearchEndpoint}?commodity_name={Uri.EscapeDataString(commodityName)}";
            _logger.LogDebug("Querying UEX API: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UEX API returned status {StatusCode} for commodity: {CommodityName}", 
                    response.StatusCode, commodityName);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Parse the commodity data from the response
            if (!root.TryGetProperty("data", out var dataArray) || dataArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("UEX API returned no data for commodity: {CommodityName}", commodityName);
                return null;
            }

            // Get first result (most relevant)
            var firstResult = dataArray[0];
            var commodity = new Commodity
            {
                Id = firstResult.TryGetProperty("id_commodity", out var id) ? id.GetInt32() : 0,
                Name = firstResult.TryGetProperty("commodity_name", out var name) ? name.GetString() ?? commodityName : commodityName,
                Type = firstResult.TryGetProperty("commodity_code", out var code) ? code.GetString() ?? "Unknown" : "Unknown"
            };

            // Build commodity prices from all results
            foreach (var priceElement in dataArray.EnumerateArray())
            {
                var price = new CommodityPrice
                {
                    Id = priceElement.TryGetProperty("id", out var priceId) ? priceId.GetInt32() : 0,
                    TerminalCode = priceElement.TryGetProperty("terminal_code", out var termCode) ? termCode.GetString() ?? "" : "",
                    TerminalName = priceElement.TryGetProperty("terminal_name", out var termName) ? termName.GetString() ?? "" : "",
                    LocationName = priceElement.TryGetProperty("planet", out var planet) ? planet.GetString() ?? "" : "",
                    BuyPrice = priceElement.TryGetProperty("price_buy", out var buy) ? (decimal)buy.GetDouble() : 0,
                    SellPrice = priceElement.TryGetProperty("price_sell", out var sell) ? (decimal)sell.GetDouble() : 0,
                    Timestamp = priceElement.TryGetProperty("time_update", out var ts) ? ts.GetString() ?? "" : ""
                };
                commodity.Prices.Add(price);
            }

            return commodity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching commodity from UEX API: {CommodityName}", commodityName);
            return null;
        }
    }

    /// <summary>
    /// Calculate commodity price summary statistics
    /// </summary>
    private static CommoditySummary CalculateSummary(Commodity commodity)
    {
        var validPrices = commodity.Prices.Where(p => p.BuyPrice > 0 || p.SellPrice > 0).ToList();

        if (!validPrices.Any())
        {
            return new CommoditySummary
            {
                CommodityName = commodity.Name,
                LocationCount = 0,
                LastUpdated = DateTime.UtcNow
            };
        }

        var minPrice = validPrices.Min(p => p.BuyPrice > 0 ? p.BuyPrice : p.SellPrice);
        var maxPrice = validPrices.Max(p => p.SellPrice > 0 ? p.SellPrice : p.BuyPrice);

        var cheapest = validPrices.FirstOrDefault(p => (p.BuyPrice > 0 ? p.BuyPrice : p.SellPrice) == minPrice);
        var expensive = validPrices.FirstOrDefault(p => (p.SellPrice > 0 ? p.SellPrice : p.BuyPrice) == maxPrice);

        return new CommoditySummary
        {
            CommodityName = commodity.Name,
            LowestPrice = minPrice,
            CheapestLocation = cheapest?.LocationName ?? "Unknown",
            HighestPrice = maxPrice,
            MostExpensiveLocation = expensive?.LocationName ?? "Unknown",
            LocationCount = commodity.Prices.Count,
            LastUpdated = DateTime.UtcNow
        };
    }
}
