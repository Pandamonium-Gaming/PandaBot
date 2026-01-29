namespace PandaBot.Models.AshesOfCreation;

public class CachedVendor
{
    public int Id { get; set; }
    public string VendorId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LocalImagePath { get; set; } = string.Empty;
    public string RawJson { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime LastUpdated { get; set; }
}
