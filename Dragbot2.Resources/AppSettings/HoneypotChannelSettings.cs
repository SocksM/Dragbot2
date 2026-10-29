namespace Dragbot2.Resources.AppSettings;

public class HoneypotChannelSettings
{
    public bool Enabled { get; init; } = true;

    public List<ulong> HoneyPotChannelIds { get; init; } = [];
    public HoneypotAction HoneypotAction { get; init; } = HoneypotAction.Kick;
    public int? TimeoutMinutes { get; init; }
    public int? DeleteMessagesMinutes { get; init; }
}
