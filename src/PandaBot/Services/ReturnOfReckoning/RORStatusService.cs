using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PandaBot.Models.ReturnOfReckoning;

namespace PandaBot.Services.ReturnOfReckoning;

/// <summary>
/// Service for fetching Return of Reckoning server status and player counts
/// </summary>
public class RORStatusService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RORStatusService> _logger;
    private const string RorWebsiteUrl = "https://www.returnofreckoning.com/";

    public RORStatusService(HttpClient httpClient, ILogger<RORStatusService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the current server status and player count from the ROR website
    /// </summary>
    public async Task<RORStatus> GetServerStatusAsync()
    {
        try
        {
            _logger.LogInformation("Fetching Return of Reckoning server status from {Url}", RorWebsiteUrl);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync(RorWebsiteUrl, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch ROR website: {StatusCode}", response.StatusCode);
                return new RORStatus
                {
                    IsOnline = false,
                    StatusMessage = $"Failed to fetch status (HTTP {response.StatusCode})"
                };
            }

            var html = await response.Content.ReadAsStringAsync(cts.Token);
            return ParseStatusFromHtml(html);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request to ROR website timed out");
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
    /// Parses the HTML from the ROR website to extract status and player count
    /// </summary>
    private RORStatus ParseStatusFromHtml(string html)
    {
        var status = new RORStatus();

        try
        {
            // Look for server status indicator classes
            // The website uses "server-status-online" or "server-status-offline" class
            if (html.Contains("server-status-online") || html.Contains("status-online"))
            {
                status.IsOnline = true;
                _logger.LogInformation("ROR server is online");
            }
            else if (html.Contains("server-status-offline") || html.Contains("status-offline"))
            {
                status.IsOnline = false;
                _logger.LogInformation("ROR server is offline");
            }

            // Try to extract player count from common patterns
            // Look for patterns like "Players: 123" or "123 Players" or similar
            var playerCountPatterns = new[]
            {
                @"(?:Players?|players?)\s*:?\s*(\d+)",  // "Players: 123" or "players 123"
                @"(\d+)\s*(?:Players?|players?)",         // "123 Players"
                @"<span[^>]*>(\d+)</span>.*?(?:player|Player)", // HTML span with number near "player"
                @"data-players=""(\d+)""",                // data attribute
                @"playercount[""']?\s*:\s*(\d+)",        // JSON-like playercount
            };

            foreach (var pattern in playerCountPatterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var playerCount))
                {
                    status.PlayerCount = playerCount;
                    _logger.LogInformation("ROR player count: {PlayerCount}", playerCount);
                    break;
                }
            }

            status.LastChecked = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing ROR website HTML");
            status.StatusMessage = "Error parsing status";
        }

        return status;
    }
}
