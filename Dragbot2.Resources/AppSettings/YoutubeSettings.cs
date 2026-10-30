namespace Dragbot2.Resources.AppSettings;

public class YoutubeSettings
{
    public bool Enabled { get; init; } = true;
    public List<YoutubeSubscription> Subscriptions { get; init; } = [];
}

public class YoutubeSubscription
{
    public string ChannelId { get; init; } = null!;
    public ulong DiscordChannelId { get; init; }
    public string MessageTemplate { get; init; } = "A new video was uploaded: [{title}]({url})";
}
