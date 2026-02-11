namespace PandaBot.Models.StarCitizen;

/// <summary>
/// Represents a location in Star Citizen with time calculation support
/// Based on VerseTime data
/// </summary>
public class VerseTimeLocation
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ParentBody { get; set; } = string.Empty;
    public string ParentStar { get; set; } = string.Empty;
    public double CoordinateX { get; set; }
    public double CoordinateY { get; set; }
    public double CoordinateZ { get; set; }
    public string? WikiLink { get; set; }
    
    /// <summary>
    /// Calculated latitude in degrees
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Calculated longitude in degrees
    /// </summary>
    public double Longitude { get; set; }
}

/// <summary>
/// Represents a celestial body (planet or moon)
/// </summary>
public class CelestialBody
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public  string? ParentBody { get; set; }
    public string ParentStar { get; set; } = string.Empty;
    public double CoordinateX { get; set; }
    public double CoordinateY { get; set; }
    public double CoordinateZ { get; set; }
    public double BodyRadius { get; set; }
    public double RotationRate { get; set; } // Seconds for full rotation
    public double RotationCorrection { get; set; } // Degrees
    public int ThemeColorR { get; set; }
    public int ThemeColorG { get; set; }
    public int ThemeColorB { get; set; }
    
    /// <summary>
    /// Length of day in Earth seconds
    /// </summary>
    public double LengthOfDay => RotationRate * 3600 / 86400;
    
    /// <summary>
    /// Angular rotation rate in degrees/minute
    /// </summary>
    public double AngularRotationRate => 6 / RotationRate;
}

/// <summary>
/// Calculated time information for a location
/// </summary>
public class LocationTimeInfo
{
    public string LocationName { get; set; } = string.Empty;
    public string ParentBody { get; set; } = string.Empty;
    public string ParentStar { get; set; } = string.Empty;
    
    /// <summary>
    /// Current local time in seconds (0-86400)
    /// </summary>
    public double LocalTime { get; set; }
    
    /// <summary>
    /// Current local time formatted (HH:MM:SS)
    /// </summary>
    public string LocalTimeFormatted { get; set; } = string.Empty;
    
    /// <summary>
    /// Current illumination status (Morning, Afternoon, Night, etc.)
    /// </summary>
    public string IlluminationStatus { get; set; } = string.Empty;
    
    /// <summary>
    /// Time until next sunrise in Earth minutes
    /// </summary>
    public double? NextStarRise { get; set; }
    
    /// <summary>
    /// Time until next sunset in Earth minutes
    /// </summary>
    public double? NextStarSet { get; set; }
    
    /// <summary>
    /// Local time when star rises (0-1, fraction of day)
    /// </summary>
    public double? LocalStarRiseTime { get; set; }
    
    /// <summary>
    /// Local time when star sets (0-1, fraction of day)
    /// </summary>
    public double? LocalStarSetTime { get; set; }
    
    /// <summary>
    /// Star altitude in degrees
    /// </summary>
    public double? StarAltitude { get; set; }
    
    /// <summary>
    /// Star azimuth in degrees
    /// </summary>
    public double? StarAzimuth { get; set; }
    
    /// <summary>
    /// Length of daylight period in Earth hours
    /// </summary>
    public double? LengthOfDaylight { get; set; }
    
    /// <summary>
    /// Real-world time for next sunrise
    /// </summary>
    public DateTime? NextSunriseRealTime { get; set; }
    
    /// <summary>
    /// Real-world time for next sunset
    /// </summary>
    public DateTime? NextSunsetRealTime { get; set; }
    
    /// <summary>
    /// Current in-game date formatted (YYYY-MM-DD)
    /// </summary>
    public string InGameDateFormatted { get; set; } = string.Empty;
    
    /// <summary>
    /// Star Citizen specific date string
    /// </summary>
    public string InGameDateString { get; set; } = string.Empty;
}
