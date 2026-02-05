namespace PandaBot.Models.StarCitizen;

/// <summary>
/// Represents a commodity's price at a specific location
/// </summary>
public class CommodityPrice
{
    public int Id { get; set; }
    public string TerminalCode { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

/// <summary>
/// Represents a commodity with its trading information
/// </summary>
public class Commodity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CommodityPrice> Prices { get; set; } = new();
}

/// <summary>
/// Represents a trade route opportunity
/// </summary>
public class TradeRoute
{
    public string CommodityName { get; set; } = string.Empty;
    public string BuyLocation { get; set; } = string.Empty;
    public string SellLocation { get; set; } = string.Empty;
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public decimal Profit { get; set; }
    public decimal ProfitMargin { get; set; }
}

/// <summary>
/// UEX API commodity search response
/// </summary>
public class UEXCommodityResponse
{
    public bool Ok { get; set; }
    public Commodity Data { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Summary of commodity prices across all locations
/// </summary>
public class CommoditySummary
{
    public string CommodityName { get; set; } = string.Empty;
    public decimal LowestPrice { get; set; }
    public string CheapestLocation { get; set; } = string.Empty;
    public decimal HighestPrice { get; set; }
    public string MostExpensiveLocation { get; set; } = string.Empty;
    public int LocationCount { get; set; }
    public DateTime LastUpdated { get; set; }
}
