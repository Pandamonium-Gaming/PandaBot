using Prometheus;

namespace PandaBot.Core;

/// <summary>
/// Prometheus metric definitions for PandaBot.
/// All metrics include a static <c>service="panda-bot"</c> label so they can be
/// joined with Loki log streams that use the same label value.
/// </summary>
internal static class BotMetrics
{
    private static readonly string[] ServiceLabelNames = ["service"];
    private static readonly string[] ServiceLabelValues = ["panda-bot"];

    /// <summary>Always 1 while the bot process is running.</summary>
    public static readonly IGauge BotUp = Metrics
        .CreateGauge(
            "bot_up",
            "1 while the bot process is running.",
            new GaugeConfiguration { LabelNames = ServiceLabelNames })
        .WithLabels(ServiceLabelValues);
}
