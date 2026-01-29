namespace PandaBot.Models.AshesOfCreation;

public class MobItemDrop
{
    public int Id { get; set; }
    public int CachedMobId { get; set; }
    public int CachedItemId { get; set; }
    public decimal? DropRate { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    
    // Navigation properties
    public CachedMob CachedMob { get; set; } = null!;
    public CachedItem CachedItem { get; set; } = null!;
}
