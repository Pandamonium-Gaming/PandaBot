using System.Text.Json;

namespace PandaBot.Utils;

/// <summary>
/// Helper utilities for working with System.Text.Json JsonElement objects.
/// Centralizes common JSON property extraction logic.
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Safely extracts a string property from a JsonElement.
    /// </summary>
    public static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    /// <summary>
    /// Safely extracts an int property from a JsonElement.
    /// </summary>
    public static int? GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetInt32();
        return null;
    }

    /// <summary>
    /// Safely extracts a bool property from a JsonElement.
    /// </summary>
    public static bool GetBoolProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.True)
            return true;
        return false;
    }
}
