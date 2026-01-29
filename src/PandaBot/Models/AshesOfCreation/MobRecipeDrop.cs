namespace PandaBot.Models.AshesOfCreation;

public class MobRecipeDrop
{
    public int Id { get; set; }
    public int CachedMobId { get; set; }
    public int CachedCraftingRecipeId { get; set; }
    public decimal? DropRate { get; set; }
    
    // Navigation properties
    public CachedMob CachedMob { get; set; } = null!;
    public CachedCraftingRecipe CachedCraftingRecipe { get; set; } = null!;
}
