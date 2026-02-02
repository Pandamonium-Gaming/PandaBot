namespace PandaBot.Models.AshesOfCreation;

public class CachedCraftingRecipe
{
    public int Id { get; set; }
    public string RecipeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Profession { get; set; } = string.Empty;
    public int ProfessionLevel { get; set; }
    public string CertificationLevel { get; set; } = string.Empty;
    public int? OutputItemCachedId { get; set; }
    public string OutputItemId { get; set; } = string.Empty;
    public string OutputItemName { get; set; } = string.Empty;
    public int OutputQuantity { get; set; }
    public string Station { get; set; } = string.Empty;
    public int CraftTime { get; set; }
    public int Views { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Navigation properties
    public CachedItem? OutputItem { get; set; }
    public ICollection<CachedRecipeIngredient> Ingredients { get; set; } = new List<CachedRecipeIngredient>();
    public ICollection<MobRecipeDrop> MobDrops { get; set; } = new List<MobRecipeDrop>();
}
