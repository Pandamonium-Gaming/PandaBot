using Discord;
using Discord.Interactions;
using DiscordBot.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DiscordBot.Modules;

/// <summary>
/// Component interaction handlers for Codex module (separate from grouped module)
/// </summary>
public class CodexComponentsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly SupabaseCodexService _codexService;
    private readonly ILogger<CodexComponentsModule> _logger;

    public CodexComponentsModule(SupabaseCodexService codexService, ILogger<CodexComponentsModule> logger)
    {
        _codexService = codexService;
        _logger = logger;
    }

    [ComponentInteraction("item_select")]
    public async Task HandleItemSelect(string[] selectedValues)
    {
        _logger.LogInformation("HandleItemSelect called with {Count} values", selectedValues.Length);
        await DeferAsync();
        
        var itemGuid = selectedValues[0];
        var item = await _codexService.GetItemByGuidAsync(itemGuid);

        if (item == null)
        {
            await FollowupAsync("Item not found");
            return;
        }

        var embed = await CodexModule.BuildItemEmbedAsync(_codexService, item);
        await FollowupAsync(embed: embed);
    }

    [ComponentInteraction("recipe_select")]
    public async Task HandleRecipeSelect(string[] selectedValues)
    {
        _logger.LogInformation("HandleRecipeSelect called with {Count} values", selectedValues.Length);
        await DeferAsync();
        
        var itemGuid = selectedValues[0];
        var item = await _codexService.GetItemByGuidAsync(itemGuid);

        if (item?.Data?.LearnableRecipes?.Any() != true)
        {
            await FollowupAsync("Recipe not found");
            return;
        }

        var embed = await CodexModule.BuildRecipeEmbedAsync(_codexService, item);
        await FollowupAsync(embed: embed);
    }

    [ComponentInteraction("creature_select")]
    public async Task HandleCreatureSelect(string[] selectedValues)
    {
        _logger.LogInformation("HandleCreatureSelect called with {Count} values", selectedValues.Length);
        await DeferAsync();
        
        var creatureGuid = selectedValues[0];
        var creature = await _codexService.GetItemByGuidAsync(creatureGuid);

        if (creature == null)
        {
            await FollowupAsync("Creature not found");
            return;
        }

        var displayName = creature.Guid.Replace("-", " ");
        var embed = new EmbedBuilder()
            .WithTitle(displayName.ToUpper())
            .WithColor(Color.Green)
            .WithDescription($"**Type:** Hunting Creature\n**ID:** {creature.Guid}")
            .Build();

        await FollowupAsync(embed: embed);
    }
}
