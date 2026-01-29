namespace PandaBot.Core.Models;

public class GuildSettings
{
    public ulong GuildId { get; set; }
    public ulong? NewsChannelId { get; set; }
    public DateTime? LastNewsCheck { get; set; }
}
