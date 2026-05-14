using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace PandaBot.Core.Services;

public class LoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly DiscordSocketClient _client;

    public LoggingService(ILogger<LoggingService> logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;
        
        _client.Log += LogAsync;
    }

    private Task LogAsync(LogMessage message)
    {
        if (ShouldSuppressGatewayNoise(message))
        {
            return Task.CompletedTask;
        }

        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        _logger.Log(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }

    private static bool ShouldSuppressGatewayNoise(LogMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Source) ||
            !message.Source.Contains("Gateway", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedMessage = message.Message?.Trim();
        return string.IsNullOrWhiteSpace(normalizedMessage) ||
               string.Equals(normalizedMessage, "null", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedMessage, "(empty message)", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedMessage, "(null)", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedMessage, "<null>", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedMessage, "[null]", StringComparison.OrdinalIgnoreCase);
    }
}
