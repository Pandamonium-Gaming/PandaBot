namespace PandaBot.Models.StarCitizen;

/// <summary>
/// Represents a vehicle from UEX Corp API
/// </summary>
public class Vehicle
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public List<VehiclePrice> Prices { get; set; } = new();
}

/// <summary>
/// Represents a price entry for a vehicle
/// </summary>
public class VehiclePrice
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string TerminalCode { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public bool OnSale { get; set; }
    public bool OnSaleWarbond { get; set; }
    public bool OnSalePackage { get; set; }
    public bool OnSaleConcierge { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Summary information for a vehicle
/// </summary>
public class VehicleSummary
{
    public decimal LowestPrice { get; set; }
    public string CheapestLocation { get; set; } = string.Empty;
    public decimal HighestPrice { get; set; }
    public string MostExpensiveLocation { get; set; } = string.Empty;
    public int LocationCount { get; set; }
    public DateTime LastUpdated { get; set; }
}
