using Dragbot2.Caches;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;

namespace Dragbot2.Services;

public class GuildUserService(
    GatewayClient client,
    ILogger<GuildUserService> logger,
    GuildUserCache guildUserCache
)
{
    public async Task<GuildUser> GetGuildUser(ulong guildId, ulong userId)
    {
        if (guildUserCache.TryGetValue(guildId, userId, out GuildUser? guildUser) && guildUser != null)
            return guildUser;
        guildUser = await client.Rest.GetGuildUserAsync(guildId, userId);
        return guildUserCache.Set(guildUser);
    }

    public async Task AddRolesToGuildUser(ulong guildId, ulong userId, List<ulong> roleIds)
    {
        if (roleIds.Count == 0) return;
        guildUserCache.Remove(guildId, userId);
        List<Task> addRoleTasks = [];
        addRoleTasks.AddRange(
            from roleId in roleIds
            select client.Rest.AddGuildUserRoleAsync(guildId, userId, roleId)
        );
        await Task.WhenAll(addRoleTasks);
        logger.LogInformation("Gave user {userId}, {roleCount} roles", userId, addRoleTasks.Count);
    }
}
