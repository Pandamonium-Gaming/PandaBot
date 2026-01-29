namespace PandaBot.Models.AshesOfCreation;

public class CachedMob
{
    public int Id { get; set; }
    public string MobId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int? Level { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LocalImagePath { get; set; } = string.Empty;
    public bool IsBoss { get; set; }
    public bool IsElite { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Navigation properties
    public ICollection<MobItemDrop> ItemDrops { get; set; } = new List<MobItemDrop>();
    public ICollection<MobRecipeDrop> RecipeDrops { get; set; } = new List<MobRecipeDrop>();
}
