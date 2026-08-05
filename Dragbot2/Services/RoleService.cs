using Dragbot2.Caches;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;

namespace Dragbot2.Services;

public class RoleService(
    GatewayClient client,
    ILogger<RoleService> logger,
    RoleCache roleCache
)
{
    public async Task<Role> GetGuildRole(ulong guildId, ulong roleId)
    {
        if (roleCache.TryGetValue(roleId, out var role) && role != null)
            return role;
        role = await client.Rest.GetGuildRoleAsync(guildId, roleId);
        return roleCache.Set(role);
    }

    public async Task<Role[]> GetGuildRoles(ulong guildId, IEnumerable<ulong> roleIds)
    {
        return await Task.WhenAll(
            roleIds.Select(async roleId => await GetGuildRole(guildId, roleId))
        );
    }
}
