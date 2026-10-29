using Microsoft.Extensions.Caching.Memory;

namespace Dragbot2.Caches;

public abstract class CustomCache<TValue>(IMemoryCache memoryCache) where TValue : class
{
    protected abstract string KeyPrefix { get; }
    protected abstract TimeSpan AbsoluteExpirationRelativeToNow { get; }
    protected abstract TimeSpan SlidingExpiration { get; }

    protected IMemoryCache MemoryCache => memoryCache;

    protected virtual string CreateKey<TKey>(TKey keySuffix) => $"{KeyPrefix}{keySuffix}";

    public virtual bool TryGetValue<TKey>(TKey id, out TValue? value)
    {
        return MemoryCache.TryGetValue(CreateKey(id), out value);
    }

    public virtual TValue Set<TKey>(TKey id, TValue value)
    {
        return MemoryCache.Set(CreateKey(id), value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
            SlidingExpiration = TimeSpan.FromHours(1),
        });
    }

    public virtual void Remove<TKey>(TKey id) =>
        MemoryCache.Remove(CreateKey(id));
}
