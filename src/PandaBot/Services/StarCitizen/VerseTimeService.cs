using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace PandaBot.Services.StarCitizen;

/// <summary>
/// Service for fetching Star Citizen location time information from VerseTime data
/// </summary>
public class VerseTimeService
{
    private const string LocationsDataUrl = "https://raw.githubusercontent.com/dydrmr/VerseTime/main/data/locations.csv";
    private const string BodiesDataUrl = "https://raw.githubusercontent.com/dydrmr/VerseTime/main/data/bodies.csv";
    private const int CacheDurationHours = 24;
    
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VerseTimeService> _logger;

    public VerseTimeService(HttpClient httpClient, IMemoryCache cache, ILogger<VerseTimeService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Search for locations by name (fuzzy matching)
    /// </summary>
    public async Task<List<Models.StarCitizen.VerseTimeLocation>> SearchLocationsAsync(string searchTerm, int maxResults = 10)
    {
        var locations = await GetAllLocationsAsync();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
            return locations.Take(maxResults).ToList();
        
        var scored = locations
            .Select(loc => new
            {
                Location = loc,
                Score = CalculateSimilarity(searchTerm.ToLower(), loc.Name.ToLower())
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => x.Location)
            .ToList();
        
        return scored;
    }

    /// <summary>
    /// Get time information for a specific location
    /// </summary>
    public async Task<Models.StarCitizen.LocationTimeInfo?> GetLocationTimeAsync(string locationName)
    {
        var locations = await GetAllLocationsAsync();
        var location = locations.FirstOrDefault(l =>
            l.Name.Equals(locationName, StringComparison.OrdinalIgnoreCase));
        
        if (location == null)
            return null;
        
        var bodies = await GetAllCelestialBodiesAsync();
        var parentBody = bodies.FirstOrDefault(b =>
            b.Name.Equals(location.ParentBody, StringComparison.OrdinalIgnoreCase));
        
        if (parentBody == null)
        {
            _logger.LogWarning("Parent body {ParentBody} not found for location {Location}",
                location.ParentBody, location.Name);
            return null;
        }
        
        // Calculate current time
        var timeInfo = CalculateLocationTime(location, parentBody);
        
        return timeInfo;
    }

    /// <summary>
    /// Get formatted time string directly from VerseTime website API
    /// This is a simpler approach than implementing all astronomical calculations
    /// </summary>
    public Task<string?> GetLocationTimeFromWebAsync(string locationName)
    {
        try
        {
            // The VerseTime website doesn't have a direct API, so we'd need to implement calculations
            // For now, return a message directing users to the website
            var result = $"Visit https://www.versetime.app/?loc={Uri.EscapeDataString(locationName)} for detailed time information";
            return Task.FromResult<string?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching time from web for {Location}", locationName);
            return Task.FromResult<string?>(null);
        }
    }

    private async Task<List<Models.StarCitizen.VerseTimeLocation>> GetAllLocationsAsync()
    {
        return await _cache.GetOrCreateAsync("versetime_locations", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
            
            try
            {
                _logger.LogInformation("Fetching VerseTime locations data from GitHub...");
                var csv = await _httpClient.GetStringAsync(LocationsDataUrl);
                return ParseLocationsCsv(csv);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching VerseTime locations data");
                return new List<Models.StarCitizen.VerseTimeLocation>();
            }
        }) ?? new List<Models.StarCitizen.VerseTimeLocation>();
    }

    private async Task<List<Models.StarCitizen.CelestialBody>> GetAllCelestialBodiesAsync()
    {
        return await _cache.GetOrCreateAsync("versetime_bodies", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
            
            try
            {
                _logger.LogInformation("Fetching VerseTime celestial bodies data from GitHub...");
                var csv = await _httpClient.GetStringAsync(BodiesDataUrl);
                return ParseBodiesCsv(csv);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching VerseTime bodies data");
                return new List<Models.StarCitizen.CelestialBody>();
            }
        }) ?? new List<Models.StarCitizen.CelestialBody>();
    }

    private List<Models.StarCitizen.VerseTimeLocation> ParseLocationsCsv(string csv)
    {
        var locations = new List<Models.StarCitizen.VerseTimeLocation>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
            return locations;
        
        // Skip header line
        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var parts = ParseCsvLine(lines[i]);
                if (parts.Length < 7)
                    continue;
                
                var location = new Models.StarCitizen.VerseTimeLocation
                {
                    Name = parts[0],
                    Type = parts[1],
                    ParentBody = parts[2],
                    ParentStar = parts[3],
                    CoordinateX = double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : 0,
                    CoordinateY = double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ? y : 0,
                    CoordinateZ = double.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ? z : 0,
                    WikiLink = parts.Length > 7 ? parts[7] : null
                };
                
                // Calculate latitude and longitude
                CalculateLatLong(location);
                
                locations.Add(location);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error parsing location CSV line {Line}", i);
            }
        }
        
        _logger.LogInformation("Parsed {Count} locations from VerseTime CSV", locations.Count);
        return locations;
    }

    private List<Models.StarCitizen.CelestialBody> ParseBodiesCsv(string csv)
    {
        var bodies = new List<Models.StarCitizen.CelestialBody>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
            return bodies;
        
        // Skip header
        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var parts = ParseCsvLine(lines[i]);
                if (parts.Length < 13)
                    continue;
                
                var body = new Models.StarCitizen.CelestialBody
                {
                    Name = parts[0],
                    Type = parts[2],
                    ParentBody = parts[3],
                    ParentStar = parts[4],
                    CoordinateX = double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : 0,
                    CoordinateY = double.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ? y : 0,
                    CoordinateZ = double.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ? z : 0,
                    BodyRadius = double.TryParse(parts[12], NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ? r : 0,
                    RotationRate = double.TryParse(parts[13], NumberStyles.Float, CultureInfo.InvariantCulture, out var rr) ? rr : 0,
                    RotationCorrection = double.TryParse(parts[14], NumberStyles.Float, CultureInfo.InvariantCulture, out var rc) ? rc : 0
                };
                
                // Parse theme colors if available
                if (parts.Length >= 21)
                {
                    body.ThemeColorR = int.TryParse(parts[18], out var cr) ? cr : 0;
                    body.ThemeColorG = int.TryParse(parts[19], out var cg) ? cg : 0;
                    body.ThemeColorB = int.TryParse(parts[20], out var cb) ? cb : 0;
                }
                
                bodies.Add(body);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error parsing body CSV line {Line}", i);
            }
        }
        
        _logger.LogInformation("Parsed {Count} celestial bodies from VerseTime CSV", bodies.Count);
        return bodies;
    }

    private void CalculateLatLong(Models.StarCitizen.VerseTimeLocation location)
    {
        // Calculate spherical coordinates
        double x = location.CoordinateX;
        double y = location.CoordinateY;
        double z = location.CoordinateZ;
        
        double r = Math.Sqrt(x * x + y * y + z * z);
        if (r == 0)
        {
            location.Latitude = 0;
            location.Longitude = 0;
            return;
        }
        
        // Latitude: arcsin(z/r)
        location.Latitude = Math.Asin(z / r) * 180.0 / Math.PI;
        
        // Longitude: atan2(y, x)
        location.Longitude = Math.Atan2(y, x) * 180.0 / Math.PI;
    }

    private Models.StarCitizen.LocationTimeInfo CalculateLocationTime(
        Models.StarCitizen.VerseTimeLocation location,
        Models.StarCitizen.CelestialBody parentBody)
    {
        // Get current universe time (time dilation: 1/4 speed)
        var now = DateTime.UtcNow;
        double universeTimeSeconds = now.Subtract(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds * 0.25;
        
        // Calculate current rotation cycle
        double currentCycle = 0;
        if (parentBody.RotationRate > 0)
        {
            currentCycle = universeTimeSeconds / parentBody.RotationRate;
        }
        
        // Calculate hour angle (current rotation position)
        double cycleFor = currentCycle - Math.Floor(currentCycle);
        double hourAngle = (360 - (cycleFor * 360) - parentBody.RotationCorrection) % 360;
        
        // Calculate local time (0-86400 seconds)
        double angle = (360 - (hourAngle +  180)) % 360;
        double localTimeFraction = angle / 360.0;
        double localTimeSeconds = 86400 * localTimeFraction;
        
        // Format time
        TimeSpan localTime = TimeSpan.FromSeconds(localTimeSeconds);
        string formatted = $"{(int)localTime.TotalHours:D2}:{localTime.Minutes:D2}:{localTime.Seconds:D2}";
        
        // Determine illumination status (simplified)
        string status = DetermineIlluminationStatus(localTimeSeconds);
        
        return new Models.StarCitizen.LocationTimeInfo
        {
            LocationName = location.Name,
            ParentBody = location.ParentBody,
            ParentStar = location.ParentStar,
            LocalTime = localTimeSeconds,
            LocalTimeFormatted = formatted,
            IlluminationStatus = status,
            // Note: Full astronomical calculations for rise/set times would require
            // implementing the complete VerseTime algorithm (declination, rise/set angles, etc.)
            // For now, we provide basic information
        };
    }

    private string DetermineIlluminationStatus(double localTimeSeconds)
    {
        var hour = localTimeSeconds / 3600.0;
        
        return hour switch
        {
            >= 23.5 or < 0.5 => "Midnight",
            >= 0.5 and < 4 => "Night",
            >= 4 and < 6 => "Morning Twilight",
            >= 6 and < 10 => "Morning",
            >= 10 and < 11.5 => "Late Morning",
            >= 11.5 and < 12.5 => "Noon",
            >= 12.5 and < 16 => "Afternoon",
            >= 16 and < 18 => "Evening",
            >= 18 and < 20 => "Evening Twilight",
            _ => "Night"
        };
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        bool inQuotes = false;
        var currentValue = new System.Text.StringBuilder();
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }
        
        values.Add(currentValue.ToString());
        return values.ToArray();
    }

    private int CalculateSimilarity(string search, string target)
    {
        // Normalize strings: remove spaces, convert to lowercase
        var normalizedSearch = search.Replace(" ", "").Replace("-", "").Replace("_", "");
        var normalizedTarget = target.Replace(" ", "").Replace("-", "").Replace("_", "");
        
        // Exact match on normalized strings
        if (normalizedTarget == normalizedSearch)
            return 10000;
        
        // Exact match on original (case-insensitive already done by caller)
        if (target == search)
            return 9000;
        
        // Check if normalized search equals normalized target exactly (highest priority)
        // This ensures "area18" matches "Area18" but not "Area19"
        if (normalizedTarget.Equals(normalizedSearch, StringComparison.Ordinal))
            return 8500;
        
        // Word-for-word match (e.g., "new babbage" matches "New Babbage")
        var searchWords = search.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var targetWords = target.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (searchWords.Length == targetWords.Length)
        {
            bool allWordsMatch = true;
            for (int i = 0; i < searchWords.Length; i++)
            {
                if (!targetWords[i].Equals(searchWords[i], StringComparison.OrdinalIgnoreCase))
                {
                    allWordsMatch = false;
                    break;
                }
            }
            if (allWordsMatch)
                return 8400;
        }
        
        // Starts with (original) - only if lengths are close to avoid "area1" matching "area19"
        if (target.StartsWith(search) && Math.Abs(normalizedTarget.Length - normalizedSearch.Length) <= 3)
            return 7000;
        
        // Starts with (normalized) - only if lengths are close
        if (normalizedTarget.StartsWith(normalizedSearch) && Math.Abs(normalizedTarget.Length - normalizedSearch.Length) <= 3)
            return 6500;
        
        // Contains (original)
        if (target.Contains(search))
            return 5000;
        
        // Contains (normalized)
        if (normalizedTarget.Contains(normalizedSearch))
            return 4500;
        
        // Word boundary match - check if search matches start of any word in target
        foreach (var word in targetWords)
        {
            if (word.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                return 4000;
        }
        
        // Levenshtein distance for fuzzy matching
        int distance = CalculateLevenshteinDistance(normalizedSearch, normalizedTarget);
        if (distance <= 2) // Allow up to 2 character differences
            return 3000 - (distance * 500);
        
        // Sequential character matching (original algorithm for very loose matches)
        int matches = 0;
        int searchIndex = 0;
        
        for (int i = 0; i < normalizedTarget.Length && searchIndex < normalizedSearch.Length; i++)
        {
            if (normalizedTarget[i] == normalizedSearch[searchIndex])
            {
                matches++;
                searchIndex++;
            }
        }
        
        return searchIndex == normalizedSearch.Length ? matches * 10 : 0;
    }

    private int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;
        
        if (string.IsNullOrEmpty(target))
            return source.Length;
        
        int sourceLength = source.Length;
        int targetLength = target.Length;
        
        // Use single array optimization
        int[] previousRow = new int[targetLength + 1];
        
        for (int i = 0; i <= targetLength; i++)
            previousRow[i] = i;
        
        for (int i = 0; i < sourceLength; i++)
        {
            int[] currentRow = new int[targetLength + 1];
            currentRow[0] = i + 1;
            
            for (int j = 0; j < targetLength; j++)
            {
                int cost = source[i] == target[j] ? 0 : 1;
                currentRow[j + 1] = Math.Min(
                    Math.Min(currentRow[j] + 1, previousRow[j + 1] + 1),
                    previousRow[j] + cost);
            }
            
            previousRow = currentRow;
        }
        
        return previousRow[targetLength];
    }
}
