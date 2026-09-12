using System.Text.Json;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace FCG.CatalogAPI.Infrastructure.Cache;

/// <summary>
/// Implementação de <see cref="ICatalogCache"/> sobre Redis via
/// <see cref="IDistributedCache"/> (Microsoft.Extensions.Caching.StackExchangeRedis),
/// serializando com System.Text.Json. Atende ao requisito de cache distribuído da Fase 3.
/// </summary>
public class RedisCatalogCache : ICatalogCache
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;

    public RedisCatalogCache(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null || bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(bytes, _json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _json);
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        await _cache.SetAsync(key, bytes, options, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct) => _cache.RemoveAsync(key, ct);
}
