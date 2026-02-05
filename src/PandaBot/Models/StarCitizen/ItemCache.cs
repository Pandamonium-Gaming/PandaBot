namespace PandaBot.Models.StarCitizen;

/// <summary>
/// Cached item data from UEX API stored in SQLite
/// </summary>
public class ItemCache
{
    public int Id { get; set; }
    public int UexItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Company { get; set; }
    public DateTime CachedAt { get; set; }
    
    /// <summary>
    /// Verify cache is still valid (24 hours = 1 day)
    /// </summary>
    public bool IsExpired => DateTime.UtcNow - CachedAt > TimeSpan.FromHours(24);

    /// <summary>
    /// Simple similarity score for fuzzy string matching (0-100)
    /// Uses Levenshtein-like approach with wildcards
    /// </summary>
    public static int SimilarityScore(string searchTerm, string itemName)
    {
        var search = searchTerm.ToLower();
        var name = itemName.ToLower();

        // Exact match = 100
        if (name == search)
            return 100;

        // Contains = 90
        if (name.Contains(search))
            return 90;

        // Starts with = 80
        if (name.StartsWith(search))
            return 80;

        // Split into words and check partial matches
        var searchWords = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var nameWords = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var matchedWords = searchWords.Count(sw => nameWords.Any(nw => nw.Contains(sw) || sw.Contains(nw)));
        if (matchedWords > 0)
        {
            return (matchedWords * 70) / searchWords.Length;
        }

        // Levenshtein-like distance
        var distance = LevenshteinDistance(search, name);
        var maxLen = Math.Max(search.Length, name.Length);
        return Math.Max(0, 100 - (distance * 100 / maxLen));
    }

    /// <summary>
    /// Calculate Levenshtein distance between two strings
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;
        var d = new int[len1 + 1, len2 + 1];

        for (var i = 0; i <= len1; i++)
            d[i, 0] = i;

        for (var j = 0; j <= len2; j++)
            d[0, j] = j;

        for (var i = 1; i <= len1; i++)
        {
            for (var j = 1; j <= len2; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[len1, len2];
    }
}
