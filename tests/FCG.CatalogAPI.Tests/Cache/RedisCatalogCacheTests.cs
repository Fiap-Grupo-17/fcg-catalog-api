using FCG.CatalogAPI.Application.Loja.Queries;
using FCG.CatalogAPI.Infrastructure.Cache;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;

namespace FCG.CatalogAPI.Tests.Cache;

/// <summary>
/// Duplo de teste em memória de <see cref="IDistributedCache"/> (sem StackExchange.Redis),
/// usado para validar deterministicamente a (de)serialização JSON e o round-trip de
/// <see cref="RedisCatalogCache"/> sem exigir um container Redis real.
/// </summary>
public class DistributedCacheEmMemoriaFake : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _armazenamento = new();

    public byte[]? Get(string key) => _armazenamento.TryGetValue(key, out var valor) ? valor : null;

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        => Task.FromResult(Get(key));

    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    public void Remove(string key) => _armazenamento.Remove(key);
    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => _armazenamento[key] = value;

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }
}

public class RedisCatalogCacheTests
{
    private static RedisCatalogCache CriarCache(out DistributedCacheEmMemoriaFake distributedCache)
    {
        distributedCache = new DistributedCacheEmMemoriaFake();
        return new RedisCatalogCache(distributedCache);
    }

    [Fact]
    public async Task SetAsync_DeveGravarValorSerializado_ERecuperarComGetAsync()
    {
        var cache = CriarCache(out _);
        var jogo = new JogoDto(Guid.NewGuid(), "Cyber Adventure", "RPG cyberpunk", "RPG", 199.90m);

        await cache.SetAsync("catalog:jogo:1", jogo, TimeSpan.FromMinutes(10), CancellationToken.None);
        var resultado = await cache.GetAsync<JogoDto>("catalog:jogo:1", CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado.Should().BeEquivalentTo(jogo);
    }

    [Fact]
    public async Task SetAsync_DeveSuportarRoundTripDeListas()
    {
        var cache = CriarCache(out _);
        var jogos = new List<JogoDto>
        {
            new(Guid.NewGuid(), "Jogo A", "desc A", "RPG", 10m),
            new(Guid.NewGuid(), "Jogo B", "desc B", "Aventura", 20m)
        };

        await cache.SetAsync("catalog:jogos:list:ativos", jogos, TimeSpan.FromMinutes(5), CancellationToken.None);
        var resultado = await cache.GetAsync<List<JogoDto>>("catalog:jogos:list:ativos", CancellationToken.None);

        resultado.Should().BeEquivalentTo(jogos);
    }

    [Fact]
    public async Task GetAsync_ChaveAusente_DeveRetornarDefault()
    {
        var cache = CriarCache(out _);

        var resultado = await cache.GetAsync<JogoDto>("chave-inexistente", CancellationToken.None);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_DeveApagarValorPreviamenteGravado()
    {
        var cache = CriarCache(out var distributedCache);
        var jogo = new JogoDto(Guid.NewGuid(), "Jogo C", "desc C", "Casual", 0m);
        await cache.SetAsync("catalog:jogo:2", jogo, TimeSpan.FromMinutes(10), CancellationToken.None);

        await cache.RemoveAsync("catalog:jogo:2", CancellationToken.None);
        var resultado = await cache.GetAsync<JogoDto>("catalog:jogo:2", CancellationToken.None);

        resultado.Should().BeNull();
        distributedCache.Get("catalog:jogo:2").Should().BeNull();
    }
}
