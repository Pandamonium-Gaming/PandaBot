using System.Text.Json;
using Microsoft.Extensions.Logging;
using PandaBot.Models.ReturnOfReckoning;

namespace PandaBot.Services.ReturnOfReckoning;

/// <summary>
/// Service for fetching Return of Reckoning server status and player counts via API
/// </summary>
public class RORStatusService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RORStatusService> _logger;
    private const string RorApiUrl = "https://api.returnofreckoning.com/stats/online_list_new.php?realm_id=1";

    public RORStatusService(HttpClient httpClient, ILogger<RORStatusService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the current server status and player count from the ROR API
    /// </summary>
    public async Task<RORStatus> GetServerStatusAsync()
    {
        try
        {
            _logger.LogInformation("Fetching Return of Reckoning server stats from {Url}", RorApiUrl);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync(RorApiUrl, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch ROR API: {StatusCode}", response.StatusCode);
                return new RORStatus
                {
                    IsOnline = false,
                    StatusMessage = $"Failed to fetch status (HTTP {response.StatusCode})"
                };
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            return ParseStatusFromJson(json);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request to ROR API timed out");
            return new RORStatus
            {
                IsOnline = false,
                StatusMessage = "Status check timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Return of Reckoning server status");
            return new RORStatus
            {
                IsOnline = false,
                StatusMessage = "Error checking status"
            };
        }
    }

    /// <summary>
    /// Parses the JSON from the ROR API to extract status and player count
    /// </summary>
    private RORStatus ParseStatusFromJson(string json)
    {
        var status = new RORStatus();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // The API should return an array of online characters
            // If we get a response, the server is online
            status.IsOnline = true;
            _logger.LogInformation("ROR server is online");

            // Count the number of characters in the array
            if (root.ValueKind == JsonValueKind.Array)
            {
                var playerCount = root.GetArrayLength();
                status.PlayerCount = playerCount;
                _logger.LogInformation("ROR online player count: {PlayerCount}", playerCount);
            }
            else if (root.TryGetProperty("count", out var countElement))
            {
                // In case the API returns a count property
                if (countElement.TryGetInt32(out var count))
                {
                    status.PlayerCount = count;
                    _logger.LogInformation("ROR online player count: {PlayerCount}", count);
                }
            }

            status.LastChecked = DateTime.UtcNow;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing ROR API response");
            // If we got a response without parsing errors, assume server is up
            status.IsOnline = true;
            status.StatusMessage = "Unable to parse player count";
            status.LastChecked = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ROR API response");
            status.StatusMessage = "Error processing response";
        }

        return status;
    }
}
