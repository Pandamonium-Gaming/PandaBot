namespace PandaBot.Models.AshesOfCreation;

public class CachedItem
{
    public int Id { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public int? Level { get; set; }
    public string IconUrl { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LocalIconPath { get; set; } = string.Empty;
    public string LocalImagePath { get; set; } = string.Empty;
    public bool IsStackable { get; set; }
    public int? MaxStackSize { get; set; }
    public string SlotType { get; set; } = string.Empty;
    public bool Enchantable { get; set; }
    public string VendorValueType { get; set; } = string.Empty;
    public int Views { get; set; }
    public string Tags { get; set; } = string.Empty; // Stored as JSON array string
    public string RawJson { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Navigation properties
    public ICollection<MobItemDrop> MobDrops { get; set; } = new List<MobItemDrop>();
}
