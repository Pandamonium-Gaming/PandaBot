namespace PandaBot.Models.ReturnOfReckoning;

/// <summary>
/// Represents the current status of the Return of Reckoning server
/// </summary>
public class RORStatus
{
    /// <summary>
    /// Whether the server is online or offline
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Current player count on the server
    /// </summary>
    public int PlayerCount { get; set; }

    /// <summary>
    /// Maximum player capacity (if available)
    /// </summary>
    public int? MaxPlayers { get; set; }

    /// <summary>
    /// Server region or name
    /// </summary>
    public string ServerName { get; set; } = "Return of Reckoning";

    /// <summary>
    /// Last time the status was checked
    /// </summary>
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Any additional status message
    /// </summary>
    public string? StatusMessage { get; set; }
}
