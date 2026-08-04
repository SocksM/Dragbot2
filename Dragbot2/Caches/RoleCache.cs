using Microsoft.Extensions.Caching.Memory;
using NetCord;

namespace Dragbot2.Caches;

public class RoleCache(IMemoryCache memoryCache) : CustomCache<Role>(memoryCache)
{
    protected override string KeyPrefix => "RoleCache_";
    protected override TimeSpan AbsoluteExpirationRelativeToNow => TimeSpan.FromDays(1);
    protected override TimeSpan SlidingExpiration => TimeSpan.FromHours(1);

    public Role Set(Role role) =>
        base.Set(CreateKey(role.Id), role);
}
