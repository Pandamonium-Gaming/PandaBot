namespace PandaBot.Models;

/// <summary>
/// Configuration for UEX Corp API
/// </summary>
public class UEXConfig
{
    public string? ApiBaseUrl { get; set; } = "https://api.uexcorp.space";
    public string? BearerToken { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
