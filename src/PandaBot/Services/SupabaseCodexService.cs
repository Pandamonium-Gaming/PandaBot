using Supabase;
using DiscordBot.Models;
using Microsoft.Extensions.Logging;
using Postgrest;
using SupabaseClient = Supabase.Client;

namespace DiscordBot.Services;

public class SupabaseCodexService
{
    private readonly SupabaseClient _supabase;
    private readonly ILogger<SupabaseCodexService> _logger;

    public SupabaseCodexService(SupabaseClient supabase, ILogger<SupabaseCodexService> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    /// <summary>
    /// Search for items by name
    /// </summary>
    public async Task<List<CodexItem>> SearchItemsByNameAsync(string searchTerm, int limit = 10)
    {
        _logger.LogInformation("SearchItemsByNameAsync called with searchTerm: {SearchTerm}, limit: {Limit}", searchTerm, limit);
        
        try
        {
            _logger.LogInformation("Building query...");
            var response = await _supabase
                .From<CodexItem>()
                .Where(x => x.Section == "items")
                .Filter("data->>itemName", Constants.Operator.ILike, $"%{searchTerm}%")
                .Limit(limit)
                .Get();

            _logger.LogInformation("Query completed. Found {Count} items", response.Models?.Count ?? 0);
            return response.Models ?? new List<CodexItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for items with term: {SearchTerm}", searchTerm);
            return new List<CodexItem>();
        }
    }

    /// <summary>
    /// Get item by exact GUID
    /// </summary>
    public async Task<CodexItem?> GetItemByGuidAsync(string guid)
    {
        _logger.LogInformation("GetItemByGuidAsync called with guid: {Guid}", guid);
        
        try
        {
            var response = await _supabase
                .From<CodexItem>()
                .Where(x => x.Guid == guid)
                .Single();

            _logger.LogInformation("Found item with guid: {Guid}", guid);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item by GUID: {Guid}", guid);
            return null;
        }
    }

    /// <summary>
    /// Search for crafting recipes by name
    /// </summary>
    public async Task<List<CodexItem>> SearchRecipesByNameAsync(string searchTerm, int limit = 10)
    {
        _logger.LogInformation("SearchRecipesByNameAsync called with searchTerm: {SearchTerm}, limit: {Limit}", searchTerm, limit);
        
        try
        {
            var response = await _supabase
                .From<CodexItem>()
                .Where(x => x.Section == "items")
                .Filter("data->_defaultRecipe", Constants.Operator.Equals, "true")
                .Filter("data->>itemName", Constants.Operator.ILike, $"%{searchTerm}%")
                .Limit(limit)
                .Get();

            _logger.LogInformation("Query completed. Found {Count} recipes", response.Models?.Count ?? 0);
            return response.Models ?? new List<CodexItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for recipes with term: {SearchTerm}", searchTerm);
            return new List<CodexItem>();
        }
    }

    /// <summary>
    /// Search for recipes by profession
    /// </summary>
    public async Task<List<CodexItem>> SearchRecipesByProfessionAsync(string profession, int limit = 20)
    {
        try
        {
            var response = await _supabase
                .From<CodexItem>()
                .Where(x => x.Section == "items")
                .Filter("data->_defaultRecipe", Constants.Operator.Equals, "true")
                .Filter("data->_learnableRecipes->0->>_profession", Constants.Operator.ILike, $"%{profession}%")
                .Limit(limit)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for recipes by profession: {Profession}", profession);
            return new List<CodexItem>();
        }
    }

    /// <summary>
    /// Get materials needed for a recipe by resolving their GUIDs to item names
    /// </summary>
    public async Task<Dictionary<string, (string ItemName, int Quantity)>> GetRecipeMaterialsAsync(LearnableRecipe recipe)
    {
        var materials = new Dictionary<string, (string ItemName, int Quantity)>();

        try
        {
            // Collect all material GUIDs
            var materialGuids = new List<string>();

            if (recipe.PrimaryResourceCosts != null)
            {
                materialGuids.AddRange(recipe.PrimaryResourceCosts
                    .Where(x => x.Item?.Guid != null)
                    .Select(x => x.Item!.Guid));
            }

            if (recipe.GeneralResourceCost != null)
            {
                materialGuids.AddRange(recipe.GeneralResourceCost
                    .Where(x => x.Item?.Guid != null)
                    .Select(x => x.Item!.Guid));
            }

            if (!materialGuids.Any())
                return materials;

            // Query for all materials at once
            var materialItems = new List<CodexItem>();
            foreach (var guid in materialGuids.Distinct())
            {
                var item = await GetItemByGuidAsync(guid);
                if (item != null)
                {
                    materialItems.Add(item);
                }
            }

            // Map primary resources
            if (recipe.PrimaryResourceCosts != null)
            {
                foreach (var cost in recipe.PrimaryResourceCosts)
                {
                    if (cost.Item?.Guid == null) continue;

                    var item = materialItems.FirstOrDefault(x => x.Guid == cost.Item.Guid);
                    var itemName = item?.Data?.ItemName ?? cost.Item.Name ?? "Unknown Item";
                    materials[$"Primary: {cost.Item.Guid}"] = (itemName, cost.Quantity);
                }
            }

            // Map general resources
            if (recipe.GeneralResourceCost != null)
            {
                foreach (var cost in recipe.GeneralResourceCost)
                {
                    if (cost.Item?.Guid == null) continue;

                    var item = materialItems.FirstOrDefault(x => x.Guid == cost.Item.Guid);
                    var itemName = item?.Data?.ItemName ?? cost.Item.Name ?? "Unknown Item";
                    materials[$"Material: {cost.Item.Guid}"] = (itemName, cost.Quantity);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving recipe materials for recipe: {RecipeName}", recipe.Name);
        }

        return materials;
    }

    /// <summary>
    /// Search for hunting creatures
    /// </summary>
    public async Task<List<CodexItem>> SearchCreaturesAsync(string searchTerm, int limit = 10)
    {
        try
        {
            // Creatures use the guid field as the name identifier
            var response = await _supabase
                .From<CodexItem>()
                .Where(x => x.Section == "hunting-creatures")
                .Filter("guid", Constants.Operator.ILike, $"%{searchTerm}%")
                .Limit(limit)
                .Get();

            _logger.LogInformation("Search for '{SearchTerm}' returned {Count} results", searchTerm, response.Models?.Count ?? 0);
            return response.Models ?? new List<CodexItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for creatures with term: {SearchTerm}", searchTerm);
            return new List<CodexItem>();
        }
    }

    /// <summary>
    /// Get drop sources for an item (which creatures/locations drop it)
    /// Uses _droppedIn field which contains location names and pOIRewardTables with mob GUIDs
    /// Cross-references mob GUIDs with the mobs section of the database
    /// </summary>
    public async Task<(List<string> enemyNames, List<string> locations, int totalDropCount)> GetItemDropSourcesAsync(string itemGuid, string itemName)
    {
        _logger.LogInformation("GetItemDropSourcesAsync called for item: {ItemGuid} ({ItemName})", itemGuid, itemName);
        
        try
        {
            // Get the item to access _droppedIn field
            var itemResult = await _supabase
                .From<CodexItem>()
                .Where(x => x.Guid == itemGuid)
                .Single();

            if (itemResult?.Data?.DroppedIn == null)
            {
                _logger.LogInformation("No drop data found for item: {ItemName}", itemName);
                return (new List<string>(), new List<string>(), 0);
            }

            var locations = new HashSet<string>();
            var mobGuids = new HashSet<string>();

            // Parse _droppedIn as JArray
            var droppedInArray = itemResult.Data.DroppedIn as Newtonsoft.Json.Linq.JArray;
            if (droppedInArray != null)
            {
                foreach (var location in droppedInArray)
                {
                    // Get location name if available
                    var locationName = location["name"]?.ToString();
                    if (!string.IsNullOrEmpty(locationName))
                    {
                        locations.Add(locationName);
                    }

                    // Get mob GUIDs from pOIRewardTables
                    var rewardTables = location["pOIRewardTables"] as Newtonsoft.Json.Linq.JArray;
                    if (rewardTables != null)
                    {
                        foreach (var table in rewardTables)
                        {
                            var mobGuid = table.ToString();
                            if (!string.IsNullOrEmpty(mobGuid))
                            {
                                mobGuids.Add(mobGuid);
                            }
                        }
                    }
                }
            }

            // Now query the mobs section to get mob names
            var enemyNames = new List<string>();
            if (mobGuids.Any())
            {
                // Query mobs section for these GUIDs
                var mobsResult = await _supabase
                    .From<CodexItem>()
                    .Where(x => x.Section == "mobs")
                    .Get();

                foreach (var mob in mobsResult.Models ?? new List<CodexItem>())
                {
                    if (mobGuids.Contains(mob.Guid))
                    {
                        var mobName = mob.Data?.ItemName ?? FormatCreatureName(mob.Guid);
                        enemyNames.Add(mobName);
                    }
                }
            }

            return (enemyNames, locations.ToList(), enemyNames.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drop sources for item: {ItemGuid}", itemGuid);
            return (new List<string>(), new List<string>(), 0);
        }
    }

    private string FormatCreatureName(string guid)
    {
        // Convert guid like "ashen-flame-sorcerer" to "Ashen Flame Sorcerer"
        return string.Join(" ", guid.Split('-').Select(word => 
            char.ToUpper(word[0]) + word.Substring(1)));
    }

    private string ExtractLocationFromName(string name)
    {
        // Extract potential location names from creature names
        // Examples: "Citadel of Steel Bloom", "Ashen Wastes", etc.
        // Look for multi-word phrases that might be locations
        
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Common location keywords
        var locationKeywords = new[] { "citadel", "wastes", "bloom", "steel", "ruins", "fortress", "keep", "tower", "cave", "den" };
        
        foreach (var keyword in locationKeywords)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    // Try to build a location name from surrounding words
                    var locationParts = new List<string>();
                    
                    // Include up to 2 words before and 2 after
                    int start = Math.Max(0, i - 2);
                    int end = Math.Min(words.Length, i + 3);
                    
                    for (int j = start; j < end; j++)
                    {
                        if (words[j].Length > 2) // Skip short words like "of", "the"
                        {
                            locationParts.Add(char.ToUpper(words[j][0]) + words[j].Substring(1));
                        }
                    }
                    
                    if (locationParts.Count > 0)
                    {
                        return string.Join(" ", locationParts);
                    }
                }
            }
        }
        
        return string.Empty;
    }
}
