using Discord;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PandaBot.Services.StarCitizen;

public class StarCitizenStatusService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StarCitizenStatusService> _logger;
    private const string StatusApiUrl = "https://status.robertsspaceindustries.com/api/v2/components.json";

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

            var components = root.GetProperty("components");
            var lastUpdated = root.GetProperty("page").GetProperty("updated_at").GetString();

            var embed = new EmbedBuilder()
                .WithTitle("🚀 Star Citizen Server Status")
                .WithColor(Color.DarkBlue)
                .WithThumbnailUrl("https://robertsspaceindustries.com/media/z2vo2a6bzzazp/source/RSI_logo.png")
                .WithTimestamp(DateTime.UtcNow);

            var statusGroups = new Dictionary<string, List<(string Name, string Status)>>();

            foreach (var component in components.EnumerateArray())
            {
                var name = component.GetProperty("name").GetString() ?? "Unknown";
                var status = component.GetProperty("status").GetString() ?? "unknown";
                var group = component.GetProperty("group").ValueKind == JsonValueKind.Null
                    ? "Other"
                    : component.GetProperty("group").GetProperty("name").GetString() ?? "Other";

                if (!statusGroups.ContainsKey(group))
                {
                    statusGroups[group] = new List<(string, string)>();
                }

                statusGroups[group].Add((name, status));
            }

            foreach (var group in statusGroups.OrderBy(x => x.Key))
            {
                var fieldValue = string.Join("\n", group.Value.Select(x => $"{GetStatusEmoji(x.Status)} {x.Name}"));
                embed.AddField(group.Key, fieldValue, inline: false);
            }

            embed.WithFooter($"Last updated: {lastUpdated}");

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
