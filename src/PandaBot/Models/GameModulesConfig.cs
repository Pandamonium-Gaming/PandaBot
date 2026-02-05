namespace PandaBot.Models;

/// <summary>
/// Configuration for enabling/disabling game modules
/// </summary>
public class GameModulesConfig
{
    /// <summary>
    /// Enable Ashes of Creation module
    /// </summary>
    public bool EnableAshesOfCreation { get; set; } = true;

    /// <summary>
    /// Enable Star Citizen module
    /// </summary>
    public bool EnableStarCitizen { get; set; } = true;

    /// <summary>
    /// Enable Path of Exile module
    /// </summary>
    public bool EnablePathOfExile { get; set; } = true;

    /// <summary>
    /// Enable Return of Reckoning module
    /// </summary>
    public bool EnableReturnOfReckoning { get; set; } = true;
}
