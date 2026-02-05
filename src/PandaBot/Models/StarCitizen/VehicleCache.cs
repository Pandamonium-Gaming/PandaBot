namespace PandaBot.Models.StarCitizen;

/// <summary>
/// SQLite cache entity for vehicles from UEX Corp
/// </summary>
public class VehicleCache
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique UEX vehicle ID
    /// </summary>
    public int UexVehicleId { get; set; }
    
    /// <summary>
    /// Vehicle name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Vehicle type (e.g., "Ship", "Ground Vehicle")
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Manufacturer name
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;
    
    /// <summary>
    /// When this entry was cached
    /// </summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Check if cache entry is expired (24 hours)
    /// </summary>
    public bool IsExpired => CachedAt < DateTime.UtcNow.AddHours(-24);
}
