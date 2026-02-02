using Discord;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PandaBot.Services.StarCitizen;

public class StarCitizenStatusService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StarCitizenStatusService> _logger;
    private const string StatusApiUrl = "https://status.robertsspaceindustries.com/index.json";

    public StarCitizenStatusService(HttpClient httpClient, ILogger<StarCitizenStatusService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Embed?> GetStatusEmbedAsync()
    {
        try
        {
            _logger.LogInformation("Fetching Star Citizen status from {Url}", StatusApiUrl);
            
            var response = await _httpClient.GetAsync(StatusApiUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var summaryStatus = root.GetProperty("summaryStatus").GetString() ?? "unknown";
            var systems = root.GetProperty("systems");

            var embed = new EmbedBuilder()
                .WithTitle("🚀 Star Citizen Server Status")
                .WithColor(Color.DarkBlue)
                .WithDescription($"**Overall Status:** {GetStatusEmoji(summaryStatus)} {summaryStatus.ToUpper()}")
                .WithThumbnailUrl(root.GetProperty("logo").GetString())
                .WithTimestamp(DateTime.UtcNow);

            foreach (var system in systems.EnumerateArray())
            {
                var name = system.GetProperty("name").GetString() ?? "Unknown";
                var status = system.GetProperty("status").GetString() ?? "unknown";
                var unresolvedIssues = system.GetProperty("unresolvedIssues").GetArrayLength();

                var statusText = $"{GetStatusEmoji(status)} {status.ToUpper()}";
                if (unresolvedIssues > 0)
                {
                    statusText += $" ({unresolvedIssues} issue{(unresolvedIssues > 1 ? "s" : "")})";
                }

                embed.AddField(name, statusText, inline: false);
            }

            var buildDate = root.GetProperty("buildDate").GetString();
            embed.WithFooter($"Last updated: {buildDate}");

            return embed.Build();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Star Citizen status");
            throw;
        }
    }

    private static string GetStatusEmoji(string status)
    {
        return status.ToLower() switch
        {
            "operational" => "✅",
            "degraded_performance" => "⚠️",
            "partial_outage" => "🔴",
            "major_outage" => "❌",
            _ => "❓"
        };
    }
}
