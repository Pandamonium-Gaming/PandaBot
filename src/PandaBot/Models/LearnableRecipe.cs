using System.Text.Json.Serialization;

namespace DiscordBot.Models;

public class LearnableRecipe
{
    [JsonPropertyName("guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_profession")]
    public string? Profession { get; set; }

    [JsonPropertyName("_professionName")]
    public string? ProfessionName { get; set; }

    [JsonPropertyName("_certificationLevel")]
    public string? CertificationLevel { get; set; }

    [JsonPropertyName("_certificationLevelMin")]
    public int? CertificationLevelMin { get; set; }

    [JsonPropertyName("_certificationLevelMax")]
    public int? CertificationLevelMax { get; set; }

    [JsonPropertyName("baseDuration")]
    public double? BaseDuration { get; set; }

    [JsonPropertyName("_rewardItems")]
    public List<RewardItem>? RewardItems { get; set; }

    [JsonPropertyName("generalResourceCost")]
    public List<ResourceCost>? GeneralResourceCost { get; set; }

    [JsonPropertyName("primaryResourceCosts")]
    public List<ResourceCost>? PrimaryResourceCosts { get; set; }

    [JsonPropertyName("_craftingCurrencyCostValue")]
    public string? CraftingCurrencyCostValue { get; set; }

    [JsonPropertyName("_craftingCurrencyCostTierId")]
    public string? CraftingCurrencyCostTierId { get; set; }
}

public class RewardItem
{
    [JsonPropertyName("guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public AmountExpression? Amount { get; set; }

    [JsonPropertyName("displayIcon")]
    [Newtonsoft.Json.JsonConverter(typeof(DiscordBot.Models.DisplayIconConverter))]
    public string? DisplayIcon { get; set; }  // TODO: Map to ashescodex.com URLs
}

public class ResourceCost
{
    [JsonPropertyName("item")]
    public ItemReference? Item { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("minRarity")]
    public string? MinRarity { get; set; }

    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }
}

public class ItemReference
{
    [JsonPropertyName("guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("typeId")]
    public long? TypeId { get; set; }
}

public class AmountExpression
{
    [JsonPropertyName("expression")]
    public string? Expression { get; set; }
}
