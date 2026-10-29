namespace Dragbot2.Resources.AppSettings;

public class AppSettings
{
    public LevelUpSettings LevelUpSettings { get; init; } = new();
    public DiscordSettings DiscordSettings { get; init; } = new();
    public CosmeticRolesSettings CosmeticRolesSettings { get; init; } = new();
}