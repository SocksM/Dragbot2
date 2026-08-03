namespace Dragbot2.Resources.AppSettings;

public class CosmeticRolesSettings
{
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Null is seen as no requirement
    /// </summary>
    public ulong? RequiredRoleIdForBasicColors { get; init; } = null;

    public List<ulong> BasicColorRoleIds { get; init; } = [];
}
