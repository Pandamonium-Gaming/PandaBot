using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Services;

/// <summary>
/// Service for interacting with the Ashes Codex API to retrieve crafting recipes and item data.
/// </summary>
public class AshesCodexService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AshesCodexService> _logger;
    private const string BaseUrl = "https://ashescodex.com";

    public AshesCodexService(HttpClient httpClient, ILogger<AshesCodexService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    /// <summary>
    /// Search for items by name or partial match
    /// </summary>
    public async Task<List<AshesItem>?> SearchItemsAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try common API endpoint patterns
            var endpoints = new[]
            {
                $"/api/items/search?q={Uri.EscapeDataString(query)}",
                $"/api/items?search={Uri.EscapeDataString(query)}",
                $"/api/search/items?query={Uri.EscapeDataString(query)}"
            };

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogInformation("Successfully found API endpoint: {Endpoint}", endpoint);
                        return JsonSerializer.Deserialize<List<AshesItem>>(json);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Failed to fetch from {Endpoint}: {Error}", endpoint, ex.Message);
                }
            }

            _logger.LogWarning("No working API endpoint found for item search");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for items with query: {Query}", query);
            return null;
        }
    }

    /// <summary>
    /// Get crafting recipe by item name or ID
    /// </summary>
    public async Task<AshesRecipe?> GetRecipeAsync(string itemNameOrId, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = new[]
            {
                $"/api/recipes/{Uri.EscapeDataString(itemNameOrId)}",
                $"/api/recipe?item={Uri.EscapeDataString(itemNameOrId)}",
                $"/api/crafting/recipe/{Uri.EscapeDataString(itemNameOrId)}"
            };

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogInformation("Successfully found recipe API endpoint: {Endpoint}", endpoint);
                        return JsonSerializer.Deserialize<AshesRecipe>(json);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Failed to fetch recipe from {Endpoint}: {Error}", endpoint, ex.Message);
                }
            }

            _logger.LogWarning("No working API endpoint found for recipe: {ItemNameOrId}", itemNameOrId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipe for: {ItemNameOrId}", itemNameOrId);
            return null;
        }
    }

    /// <summary>
    /// Get all recipes (paginated)
    /// </summary>
    public async Task<List<AshesRecipe>?> GetAllRecipesAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = new[]
            {
                $"/api/recipes?page={page}",
                $"/api/recipes/all?page={page}",
                $"/api/items/consumable/recipe?page={page}"
            };

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogInformation("Successfully found recipes list API endpoint: {Endpoint}", endpoint);
                        return JsonSerializer.Deserialize<List<AshesRecipe>>(json);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Failed to fetch recipes from {Endpoint}: {Error}", endpoint, ex.Message);
                }
            }

            _logger.LogWarning("No working API endpoint found for recipes list");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all recipes");
            return null;
        }
    }
}

#region Models

public class AshesItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("level")]
    public int? Level { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class AshesRecipe
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("output")]
    public AshesItem? Output { get; set; }

    [JsonPropertyName("ingredients")]
    public List<RecipeIngredient>? Ingredients { get; set; }

    [JsonPropertyName("profession")]
    public string? Profession { get; set; }

    [JsonPropertyName("level")]
    public int? Level { get; set; }

    [JsonPropertyName("station")]
    public string? Station { get; set; }
}

public class RecipeIngredient
{
    [JsonPropertyName("item")]
    public AshesItem? Item { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

#endregion
