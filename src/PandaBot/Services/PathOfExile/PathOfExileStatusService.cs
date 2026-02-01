using Discord;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PandaBot.Services.PathOfExile;

public class PathOfExileStatusService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PathOfExileStatusService> _logger;
    private const string StatusApiUrl = "https://status.poe.com/api/v2/status.json";

    public PathOfExileStatusService(HttpClient httpClient, ILogger<PathOfExileStatusService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Embed?> GetStatusEmbedAsync()
    {
        try
        {
            _logger.LogInformation("Fetching Path of Exile status from {Url}", StatusApiUrl);
            
            var response = await _httpClient.GetAsync(StatusApiUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Get status from the status object
            var statusObj = root.GetProperty("status");
            var statusText = statusObj.GetProperty("description").GetString() ?? "unknown";
            var statusIndicator = statusObj.GetProperty("indicator").GetString() ?? "unknown";

            var embed = new EmbedBuilder()
                .WithTitle("⚔️ Path of Exile Server Status")
                .WithColor(GetColorForStatus(statusIndicator))
                .WithDescription($"**Overall Status:** {GetStatusEmoji(statusIndicator)} {statusText}")
                .WithTimestamp(DateTime.UtcNow);

            // List components (game servers, web, etc.)
            if (root.TryGetProperty("components", out var componentsArray))
            {
                var operationalComponents = new List<string>();
                var degradedComponents = new List<string>();
                var downComponents = new List<string>();

                foreach (var component in componentsArray.EnumerateArray())
                {
                    var name = component.GetProperty("name").GetString() ?? "Unknown";
                    var status = component.GetProperty("status").GetString() ?? "operational";

                    if (status == "operational")
                        operationalComponents.Add($"✅ {name}");
                    else if (status == "degraded_performance")
                        degradedComponents.Add($"⚠️ {name}");
                    else if (status == "major_outage")
                        downComponents.Add($"🔴 {name}");
                }

                if (downComponents.Count > 0)
                {
                    embed.AddField("🔴 Major Outage", string.Join("\n", downComponents), inline: false);
                }

                if (degradedComponents.Count > 0)
                {
                    embed.AddField("⚠️ Degraded Performance", string.Join("\n", degradedComponents), inline: false);
                }

                if (operationalComponents.Count > 0)
                {
                    embed.AddField("✅ Operational", string.Join("\n", operationalComponents), inline: false);
                }
            }

            embed.WithFooter("PoE Status | Last checked");

            return embed.Build();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching Path of Exile status");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for Path of Exile status");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching Path of Exile status");
            return null;
        }
    }

    private static string GetStatusEmoji(string? status)
    {
        return (status?.ToLower()) switch
        {
            "operational" => "✅",
            "degraded_performance" => "⚠️",
            "major_outage" => "🔴",
            "investigating" => "🔍",
            "identified" => "🔍",
            _ => "❓"
        };
    }

    private static Color GetColorForStatus(string? status)
    {
        return (status?.ToLower()) switch
        {
            "operational" => Color.Green,
            "degraded_performance" => Color.Orange,
            "major_outage" => Color.Red,
            "investigating" => Color.Gold,
            "identified" => Color.Gold,
            _ => Color.DarkGrey
        };
    }
}
