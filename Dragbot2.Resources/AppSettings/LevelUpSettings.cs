namespace Dragbot2.Resources.AppSettings;

public class LevelUpSettings
{
    public bool Enabled { get; init; } = true;
    /// <summary>
    /// key is the role, value is the level requirement
    /// </summary>
    public Dictionary<ulong, int> LevelRoleRequirements { get; init; } = new();
    public string LevelUpRegex { get; init; } = "";
    /// <summary>
    /// If there are no entries it's assumed that all channels are allowed.
    /// </summary>
    public List<ulong> AllowedChannelIds { get; init; } = [];
    public List<ulong> AllowedUserIds { get; init; } = [];

    public int UserIdRegexCaptureGroupIndex
    {
        get;
        init
        {
            if (!Enabled) return;
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(UserIdRegexCaptureGroupIndex));
            field = value;
        }
    } = -1;

    public int LevelRegexCaptureGroupIndex
    {
        get;
        init
        {
            if (!Enabled) return;
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(LevelRegexCaptureGroupIndex));
            field = value;
        }
    } = -1;
}
