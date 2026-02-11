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
    /// Cache schema version - used to invalidate cache when data structure changes
    /// Current version: 2 (added proper vehicle type derivation)
    /// </summary>
    public int CacheVersion { get; set; } = 2;
    
    /// <summary>
    /// Check if cache entry is expired (24 hours)
    /// </summary>
    public bool IsExpired => CachedAt < DateTime.UtcNow.AddHours(-24);
    
    /// <summary>
    /// Expected cache version for current code
    /// </summary>
    public const int CurrentCacheVersion = 2;
    
    /// <summary>
    /// Check if cache entry is outdated due to version mismatch
    /// </summary>
    public bool IsOutdated => CacheVersion < CurrentCacheVersion;
}
