namespace PandaBot.Models.AshesOfCreation;

public class CachedRecipeIngredient
{
    public int Id { get; set; }
    public int CachedCraftingRecipeId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    
    // Navigation property
    public CachedCraftingRecipe CachedCraftingRecipe { get; set; } = null!;
}
