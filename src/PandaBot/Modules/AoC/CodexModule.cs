using Discord;
using Discord.Interactions;
using DiscordBot.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DiscordBot.Modules;

[Group("codex", "Search the Ashes of Creation codex")]
public class CodexModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly SupabaseCodexService _codexService;
    private readonly ILogger<CodexModule> _logger;

    public CodexModule(SupabaseCodexService codexService, ILogger<CodexModule> logger)
    {
        _codexService = codexService;
        _logger = logger;
    }

    [SlashCommand("item", "Search for an item in the codex")]
    public async Task SearchItemAsync(
        [Summary("name", "The name of the item to search for")] string itemName,
        [Summary("exact", "Use exact match instead of fuzzy search")] bool exactMatch = false,
        [Summary("type", "Filter by item type/subtype")] string? itemType = null,
        [Summary("min-level", "Minimum item level")] int? minLevel = null)
    {
        _logger.LogInformation("SearchItemAsync called with itemName: {ItemName}, exact: {Exact}, type: {Type}, minLevel: {MinLevel}", 
            itemName, exactMatch, itemType, minLevel);
        
        try
        {
            await DeferAsync();
            _logger.LogInformation("Deferred response sent");

            _logger.LogInformation("Calling SearchItemsByNameAsync...");
            var items = await _codexService.SearchItemsByNameAsync(itemName, exactMatch ? 50 : 10);
            _logger.LogInformation("Found {Count} items", items?.Count ?? 0);

            if (items == null || !items.Any())
            {
                await FollowupAsync($"No items found matching '{itemName}'");
                return;
            }

            // Apply filters
            if (exactMatch)
            {
                items = items.Where(i => i.Data?.ItemName?.Equals(itemName, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            if (!string.IsNullOrEmpty(itemType))
            {
                items = items.Where(i => i.Data?.SubType?.Contains(itemType, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            if (minLevel.HasValue)
            {
                items = items.Where(i => i.Data?.Level >= minLevel.Value).ToList();
            }

            if (!items.Any())
            {
                await FollowupAsync($"No items found matching your criteria");
                return;
            }

            if (items.Count == 1)
            {
                var item = items[0];
                var embed = await BuildItemEmbedAsync(_codexService, item);
                await FollowupAsync(embed: embed);
                _logger.LogInformation("Sent single item response");
            }
            else
            {
                // Build select menu for multiple results
                var selectMenu = new SelectMenuBuilder()
                    .WithCustomId("item_select")
                    .WithPlaceholder("Choose an item to view details")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var item in items.Take(25)) // Discord limit
                {
                    var label = item.Data?.ItemName ?? "Unknown";
                    var description = $"{item.Data?.Grade ?? "?"} - Lvl {item.Data?.Level ?? 0}";
                    if (!string.IsNullOrEmpty(item.Data?.SubType))
                        description += $" - {item.Data.SubType}";
                    
                    selectMenu.AddOption(
                        label: label.Length > 100 ? label.Substring(0, 97) + "..." : label,
                        value: item.Guid,
                        description: description.Length > 100 ? description.Substring(0, 97) + "..." : description
                    );
                }

                var component = new ComponentBuilder()
                    .WithSelectMenu(selectMenu)
                    .Build();

                var embed = new EmbedBuilder()
                    .WithTitle($"Found {items.Count} item(s) matching '{itemName}'")
                    .WithColor(Color.Blue)
                    .WithDescription("Select an item below to view full details")
                    .Build();

                await FollowupAsync(embed: embed, components: component);
                _logger.LogInformation("Sent multiple items response with select menu");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchItemAsync for itemName: {ItemName}", itemName);
            try
            {
                await FollowupAsync($"An error occurred while searching for items: {ex.Message}");
            }
            catch (Exception followupEx)
            {
                _logger.LogError(followupEx, "Failed to send error followup message");
            }
        }
    }

    [SlashCommand("recipe", "Search for a crafting recipe")]
    public async Task SearchRecipeAsync(
        [Summary("name", "The name of the recipe or item to search for")] string recipeName,
        [Summary("profession", "Filter by profession")] string? profession = null)
    {
        _logger.LogInformation("SearchRecipeAsync called with recipeName: {RecipeName}, profession: {Profession}", recipeName, profession);
        
        try
        {
            await DeferAsync();
            _logger.LogInformation("Deferred response sent");

            _logger.LogInformation("Calling SearchRecipesByNameAsync...");
            var recipes = await _codexService.SearchRecipesByNameAsync(recipeName, 25);
            _logger.LogInformation("Found {Count} recipes", recipes?.Count ?? 0);

            if (recipes == null || !recipes.Any())
            {
                await FollowupAsync($"No recipes found matching '{recipeName}'");
                return;
            }

            // Apply profession filter
            if (!string.IsNullOrEmpty(profession))
            {
                recipes = recipes.Where(r => 
                    r.Data?.LearnableRecipes?.Any(lr => 
                        lr.ProfessionName?.Contains(profession, StringComparison.OrdinalIgnoreCase) == true) == true
                ).ToList();
            }

            if (!recipes.Any())
            {
                await FollowupAsync($"No recipes found matching your criteria");
                return;
            }

            if (recipes.Count == 1 && recipes[0].Data?.LearnableRecipes?.Any() == true)
            {
                var recipe = recipes[0];
                _logger.LogInformation("Building recipe embed...");
                var embed = await BuildRecipeEmbedAsync(_codexService, recipe);
                await FollowupAsync(embed: embed);
                _logger.LogInformation("Sent single recipe response");
            }
            else
            {
                // Build select menu
                var selectMenu = new SelectMenuBuilder()
                    .WithCustomId("recipe_select")
                    .WithPlaceholder("Choose a recipe to view details")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var recipe in recipes.Take(25))
                {
                    var recipeData = recipe.Data?.LearnableRecipes?.FirstOrDefault();
                    var label = recipeData?.RewardItems?.FirstOrDefault()?.ItemName ?? recipe.Data?.ItemName ?? "Unknown";
                    var description = $"{recipeData?.ProfessionName ?? "Unknown"} - Lvl {recipeData?.CertificationLevel ?? "?"}";
                    
                    selectMenu.AddOption(
                        label: label.Length > 100 ? label.Substring(0, 97) + "..." : label,
                        value: recipe.Guid,
                        description: description.Length > 100 ? description.Substring(0, 97) + "..." : description
                    );
                }

                var component = new ComponentBuilder()
                    .WithSelectMenu(selectMenu)
                    .Build();

                var embed = new EmbedBuilder()
                    .WithTitle($"Found {recipes.Count} recipe(s) matching '{recipeName}'")
                    .WithColor(Color.Green)
                    .WithDescription("Select a recipe below to view full details")
                    .Build();

                await FollowupAsync(embed: embed, components: component);
                _logger.LogInformation("Sent multiple recipes response with select menu");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchRecipeAsync for recipeName: {RecipeName}", recipeName);
            try
            {
                await FollowupAsync($"An error occurred while searching for recipes: {ex.Message}");
            }
            catch (Exception followupEx)
            {
                _logger.LogError(followupEx, "Failed to send error followup message");
            }
        }
    }

    [SlashCommand("profession", "List recipes for a specific profession")]
    public async Task SearchByProfessionAsync(
        [Summary("profession", "The profession to search for (e.g., Scribe, Blacksmith)")]
        [Autocomplete(typeof(ProfessionAutocompleteHandler))]
        string profession)
    {
        await DeferAsync();

        var recipes = await _codexService.SearchRecipesByProfessionAsync(profession, 10);

        if (recipes == null || !recipes.Any())
        {
            await FollowupAsync($"No recipes found for profession '{profession}'");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Found {recipes.Count} recipe(s) for **{profession}**:\n");

        foreach (var recipe in recipes.Take(10))
        {
            var recipeData = recipe.Data?.LearnableRecipes?.FirstOrDefault();
            if (recipeData != null)
            {
                var itemName = recipeData.RewardItems?.FirstOrDefault()?.ItemName ?? "Unknown";
                var level = recipeData.CertificationLevel ?? "?";
                sb.AppendLine($"• **{itemName}** ({level})");
            }
        }

        var embed = new EmbedBuilder()
            .WithTitle($"{profession} Recipes")
            .WithDescription(sb.ToString())
            .WithColor(Color.Gold)
            .WithFooter($"Use /codex recipe <name> for details")
            .Build();

        await FollowupAsync(embed: embed);
    }

    [SlashCommand("creature", "Search for a hunting creature")]
    public async Task SearchCreatureAsync(
        [Summary("name", "The name of the creature to search for")] string creatureName)
    {
        _logger.LogInformation("SearchCreatureAsync STARTED at {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
        await DeferAsync();

        var creatures = await _codexService.SearchCreaturesAsync(creatureName, 25);

        if (creatures == null || !creatures.Any())
        {
            await FollowupAsync($"No creatures found matching '{creatureName}'");
            return;
        }

        if (creatures.Count == 1)
        {
            var creature = creatures[0];
            var embed = new EmbedBuilder()
                .WithTitle(creature.Guid.Replace("-", " ").ToUpper())
                .WithColor(Color.Red)
                .WithDescription($"**Type:** Hunting Creature\n**ID:** {creature.Guid}")
                .Build();

            await FollowupAsync(embed: embed);
        }
        else
        {
            // Build select menu
            var selectMenu = new SelectMenuBuilder()
                .WithCustomId("creature_select")
                .WithPlaceholder("Choose a creature to view details")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var creature in creatures.Take(25))
            {
                var displayName = creature.Guid.Replace("-", " ");
                selectMenu.AddOption(
                    label: displayName.Length > 100 ? displayName.Substring(0, 97) + "..." : displayName,
                    value: creature.Guid,
                    description: "Hunting Creature"
                );
            }

            var component = new ComponentBuilder()
                .WithSelectMenu(selectMenu)
                .Build();

            var embed = new EmbedBuilder()
                .WithTitle($"Found {creatures.Count} creature(s) matching '{creatureName}'")
                .WithColor(Color.Red)
                .WithDescription("Select a creature below to view details")
                .Build();

            await FollowupAsync(embed: embed, components: component);
        }
    }

    public static async Task<Embed> BuildItemEmbedAsync(SupabaseCodexService codexService, Models.CodexItem item)
    {
        var data = item.Data;
        var embed = new EmbedBuilder()
            .WithTitle(data?.ItemName ?? "Unknown Item")
            .WithColor(GetRarityColor(data?.RarityMax))
            .WithDescription(data?.Summary ?? "No description available");

        if (!string.IsNullOrEmpty(data?.Grade))
            embed.AddField("Grade", data.Grade, inline: true);

        if (data?.Level.HasValue == true)
            embed.AddField("Level", data.Level.Value.ToString(), inline: true);

        if (!string.IsNullOrEmpty(data?.RarityMin) || !string.IsNullOrEmpty(data?.RarityMax))
        {
            var rarity = data.RarityMin == data.RarityMax
                ? data.RarityMax
                : $"{data.RarityMin} - {data.RarityMax}";
            embed.AddField("Rarity", rarity, inline: true);
        }

        if (!string.IsNullOrEmpty(data?.SubType) && data.SubType != "None")
            embed.AddField("Type", data.SubType, inline: true);

        // Add drop source information
        var (enemyNames, locations, dropCount) = await codexService.GetItemDropSourcesAsync(item.Guid, data?.ItemName ?? "");
        
        if (locations.Any())
        {
            embed.AddField("Found In", string.Join(", ", locations.Distinct()), inline: false);
        }
        
        if (enemyNames.Any())
        {
            var enemyText = dropCount > 5 
                ? $"{string.Join(", ", enemyNames.Take(5))} (+{dropCount - 5} more)" 
                : string.Join(", ", enemyNames);
            embed.AddField($"Dropped By ({dropCount} {(dropCount == 1 ? "enemy" : "enemies")})", enemyText, inline: false);
        }

        // Add tags if available
        if (data?.Tags?.Tags != null && data.Tags.Tags.Any())
        {
            var tags = string.Join(", ", data.Tags.Tags.Take(5).Select(t => t.TagName?.Split('.').Last() ?? ""));
            if (!string.IsNullOrEmpty(tags))
                embed.AddField("Tags", tags);
        }

        return embed.Build();
    }

    public static async Task<Embed> BuildRecipeEmbedAsync(SupabaseCodexService codexService, Models.CodexItem recipeItem)
    {
        var recipe = recipeItem.Data?.LearnableRecipes?.FirstOrDefault();
        if (recipe == null)
        {
            return new EmbedBuilder()
                .WithTitle("Recipe Not Found")
                .WithDescription("No recipe data available")
                .WithColor(Color.Red)
                .Build();
        }

        var rewardItem = recipe.RewardItems?.FirstOrDefault();
        var embed = new EmbedBuilder()
            .WithTitle($"Recipe: {rewardItem?.ItemName ?? "Unknown"}")
            .WithColor(Color.Green);

        if (!string.IsNullOrEmpty(recipe.ProfessionName))
            embed.AddField("Profession", recipe.ProfessionName, inline: true);

        if (!string.IsNullOrEmpty(recipe.CertificationLevel))
            embed.AddField("Certification", recipe.CertificationLevel, inline: true);

        if (recipe.CertificationLevelMin.HasValue && recipe.CertificationLevelMax.HasValue)
            embed.AddField("Level Range", $"{recipe.CertificationLevelMin} - {recipe.CertificationLevelMax}", inline: true);

        // Add crafting time
        if (recipe.BaseDuration.HasValue)
            embed.AddField("Crafting Time", $"{recipe.BaseDuration}s", inline: true);

        // Add currency cost
        if (!string.IsNullOrEmpty(recipe.CraftingCurrencyCostValue))
            embed.AddField("Cost", $"{recipe.CraftingCurrencyCostValue} gold", inline: true);

        // Add what you get
        if (rewardItem != null)
        {
            var amount = rewardItem.Amount?.Expression ?? "1";
            embed.AddField("Produces", $"{amount}x {rewardItem.ItemName}");
        }

        // Get and add materials
        var materials = await codexService.GetRecipeMaterialsAsync(recipe);
        if (materials.Any())
        {
            var materialText = new StringBuilder();
            foreach (var (key, (itemName, quantity)) in materials)
            {
                materialText.AppendLine($"• {quantity}x {itemName}");
            }
            embed.AddField("Materials Required", materialText.ToString());
        }

        return embed.Build();
    }

    public static Color GetRarityColor(string? rarity)
    {
        return rarity?.ToLower() switch
        {
            "legendary" => new Color(255, 128, 0),
            "epic" => new Color(163, 53, 238),
            "rare" => new Color(0, 112, 221),
            "uncommon" => new Color(30, 255, 0),
            "common" or "1" => Color.LightGrey,
            _ => Color.Default
        };
    }
}

// Autocomplete handler for professions
public class ProfessionAutocompleteHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var professions = new[]
        {
            "Scribe",
            "Blacksmith",
            "Carpenter",
            "Leatherworker",
            "Tailor",
            "Alchemist",
            "Cook",
            "Jeweler"
        };

        var userInput = autocompleteInteraction.Data.Current.Value?.ToString()?.ToLower() ?? "";

        var results = professions
            .Where(p => p.ToLower().Contains(userInput))
            .Select(p => new AutocompleteResult(p, p))
            .Take(25);

        return Task.FromResult(AutocompletionResult.FromSuccess(results));
    }
}
