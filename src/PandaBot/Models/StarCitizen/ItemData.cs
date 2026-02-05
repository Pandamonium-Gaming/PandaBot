namespace PandaBot.Models.StarCitizen;

/// <summary>
/// Represents an item (ship component, weapon, armor, etc.)
/// </summary>
public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Company { get; set; }
    public List<ItemPrice> Prices { get; set; } = new();
}

/// <summary>
/// Represents an item's price at a specific location
/// </summary>
public class ItemPrice
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string TerminalCode { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

/// <summary>
/// Summary of item pricing information
/// </summary>
public class ItemSummary
{
    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int LocationCount { get; set; }
    public decimal LowestPrice { get; set; }
    public decimal HighestPrice { get; set; }
    public string CheapestLocation { get; set; } = string.Empty;
    public string MostExpensiveLocation { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
