namespace Dragbot2.Resources;

[Flags]
public enum HoneypotAction
{
    None = 0,
    Kick = 1 << 0,
    Ban = 1 << 1,
    DeleteRecentMessages = 1 << 2,
    Timeout = 1 << 3,
}
