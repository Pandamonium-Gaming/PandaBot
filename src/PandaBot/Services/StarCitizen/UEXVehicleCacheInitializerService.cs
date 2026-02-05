using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PandaBot.Core.Data;
using PandaBot.Models.StarCitizen;
using System.Text.Json;

namespace PandaBot.Services.StarCitizen;

/// <summary>
/// Background service that initializes the UEX vehicle cache at startup
/// </summary>
public class UEXVehicleCacheInitializerService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UEXVehicleCacheInitializerService> _logger;
    private const string VehiclesEndpoint = "https://api.uexcorp.uk/2.0/vehicles";

    public UEXVehicleCacheInitializerService(IServiceProvider serviceProvider, ILogger<UEXVehicleCacheInitializerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting UEX vehicle cache initialization...");
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PandaBotContext>();
            var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

            // Check if cache is already populated
            var existingCount = await context.UexVehicleCache.CountAsync(cancellationToken);
            if (existingCount > 0)
            {
                _logger.LogInformation("Vehicle cache already populated with {Count} vehicles", existingCount);
                return;
            }

            _logger.LogInformation("Fetching vehicles from UEX API...");
            var vehicles = await FetchAllVehiclesAsync(httpClient);

            if (!vehicles.Any())
            {
                _logger.LogWarning("No vehicles returned from API");
                return;
            }

            _logger.LogInformation("Caching {Count} vehicles to database...", vehicles.Count);

            // Cache in batches
            const int batchSize = 100;
            for (int i = 0; i < vehicles.Count; i += batchSize)
            {
                var batch = vehicles.Skip(i).Take(batchSize);
                foreach (var vehicle in batch)
                {
                    var cacheEntry = new VehicleCache
                    {
                        UexVehicleId = vehicle.Id,
                        Name = vehicle.Name,
                        Type = vehicle.Type,
                        Manufacturer = vehicle.Manufacturer,
                        CachedAt = DateTime.UtcNow
                    };
                    context.UexVehicleCache.Add(cacheEntry);
                }
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Cached {BatchStart}-{BatchEnd} vehicles", i + 1, Math.Min(i + batchSize, vehicles.Count));
            }

            _logger.LogInformation("✅ Vehicle cache initialization complete - {Count} vehicles cached", vehicles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing UEX vehicle cache");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Derive vehicle type from boolean flags in API response
    /// </summary>
    private static string DeriveVehicleType(JsonElement vehicleElement)
    {
        // Check for ground vehicle first
        if (GetBoolProperty(vehicleElement, "is_ground_vehicle"))
            return "Ground Vehicle";

        // Check for specific ship types
        if (GetBoolProperty(vehicleElement, "is_cargo"))
            return "Cargo Ship";
        if (GetBoolProperty(vehicleElement, "is_mining"))
            return "Mining Ship";
        if (GetBoolProperty(vehicleElement, "is_medical"))
            return "Medical Ship";
        if (GetBoolProperty(vehicleElement, "is_refuel"))
            return "Refueling Ship";
        if (GetBoolProperty(vehicleElement, "is_repair"))
            return "Repair Ship";
        if (GetBoolProperty(vehicleElement, "is_exploration"))
            return "Exploration Ship";
        if (GetBoolProperty(vehicleElement, "is_racing"))
            return "Racing Ship";
        if (GetBoolProperty(vehicleElement, "is_bomber"))
            return "Bomber";
        if (GetBoolProperty(vehicleElement, "is_military"))
            return "Military Ship";
        if (GetBoolProperty(vehicleElement, "is_passenger"))
            return "Passenger Ship";

        // Default to spaceship if no specific type found
        if (GetBoolProperty(vehicleElement, "is_spaceship"))
            return "Spaceship";

        return "Vehicle";
    }

    /// <summary>
    /// Helper to safely get boolean property from JSON element
    /// </summary>
    private static bool GetBoolProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.TryGetInt32(out int intValue))
                return intValue != 0;
            if (prop.ValueKind == JsonValueKind.True)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Fetch all vehicles from API
    /// </summary>
    private async Task<List<Vehicle>> FetchAllVehiclesAsync(HttpClient httpClient)
    {
        try
        {
            _logger.LogInformation("Fetching vehicles from {Endpoint}", VehiclesEndpoint);
            var response = await httpClient.GetAsync(VehiclesEndpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("API returned {StatusCode}", response.StatusCode);
                return new();
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataArray) || dataArray.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                _logger.LogError("Invalid API response structure - no 'data' array");
                return new();
            }

            var vehicles = new List<Vehicle>();
            foreach (var vehicleElement in dataArray.EnumerateArray())
            {
                // Derive vehicle type from boolean flags
                var vehicleType = DeriveVehicleType(vehicleElement);

                var vehicle = new Vehicle
                {
                    Id = vehicleElement.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                    Name = vehicleElement.TryGetProperty("name", out var name) ? name.GetString() ?? "Unknown" : "Unknown",
                    Type = vehicleType,
                    Manufacturer = vehicleElement.TryGetProperty("company_name", out var mfg) ? mfg.GetString() ?? "" : ""
                };
                vehicles.Add(vehicle);
            }

            _logger.LogInformation("Fetched {Count} vehicles from API", vehicles.Count);
            return vehicles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vehicles from API");
            return new();
        }
    }
}
