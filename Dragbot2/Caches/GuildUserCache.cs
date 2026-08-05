using Microsoft.Extensions.Caching.Memory;
using NetCord;

namespace Dragbot2.Caches;

public class GuildUserCache(IMemoryCache memoryCache) : CustomCache<GuildUser>(memoryCache)
{
    protected override string KeyPrefix => "GuildUserCache_";
    protected override TimeSpan AbsoluteExpirationRelativeToNow => TimeSpan.FromHours(1);
    protected override TimeSpan SlidingExpiration => TimeSpan.FromMinutes(5);

    private static string ConcatIds(ulong guildId, ulong userId) =>
        $"{guildId}-{userId}";

    private static void ThrowInvalidOpEx() =>
        throw new InvalidOperationException("GuildUserCache requires both the guildId and the userId, use the overloads.");

    public override bool TryGetValue<TKey>(TKey id, out GuildUser? value)
    {
        ThrowInvalidOpEx();
        value = null;
        return false;
    }

    public bool TryGetValue(ulong guildId, ulong userId, out GuildUser? value) =>
        base.TryGetValue(ConcatIds(guildId, userId), out value);

    public override GuildUser Set<TKey>(TKey id, GuildUser value)
    {
        ThrowInvalidOpEx();
        return null!;
    }

    public GuildUser Set(ulong guildId, ulong userId, GuildUser value) =>
        base.Set(ConcatIds(guildId, userId), value);

    public GuildUser Set(GuildUser guildUser) =>
        base.Set(ConcatIds(guildUser.GuildId, guildUser.Id), guildUser);

    public override void Remove<TKey>(TKey id)
    {
        ThrowInvalidOpEx();
    }

    public void Remove(ulong guildId, ulong userId) =>
        base.Remove(ConcatIds(guildId, userId));
}
