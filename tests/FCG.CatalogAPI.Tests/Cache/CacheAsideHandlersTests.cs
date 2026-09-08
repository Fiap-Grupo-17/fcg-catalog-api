using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Queries;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using FCG.CatalogAPI.Infrastructure.Cache;
using FCG.CatalogAPI.Tests.Application;
using FluentAssertions;

namespace FCG.CatalogAPI.Tests.Cache;

/// <summary>
/// Fake de <see cref="ICatalogCache"/> em memória (sem Redis), usado para exercitar
/// deterministicamente o comportamento de cache-aside dos decorators de leitura.
/// Prefere-se um fake manual a um mock, pois a interface é simples e o comportamento
/// de armazenamento real (não apenas "foi chamado") é o que queremos validar.
/// </summary>
public class CatalogCacheFake : ICatalogCache
{
    private readonly Dictionary<string, object?> _armazenamento = new();

    public int ChamadasSet { get; private set; }
    public int ChamadasRemove { get; private set; }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct)
        => Task.FromResult(_armazenamento.TryGetValue(key, out var valor) ? (T?)valor : default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        ChamadasSet++;
        _armazenamento[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct)
    {
        ChamadasRemove++;
        _armazenamento.Remove(key);
        return Task.CompletedTask;
    }
}

public class CacheAsideHandlersTests
{
    private static async Task<TestCatalogDbContext> DbComJogo(Jogo jogo)
    {
        var db = TestCatalogDbContext.Criar();
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);
        return db;
    }

    // ---- CachedListarJogosHandler ----

    [Fact]
    public async Task Listar_PrimeiraChamada_DeveSerMissEGravarNoCache()
    {
        var jogo = Jogo.Criar("Cyber Adventure", "RPG cyberpunk", "RPG", 199.90m);
        using var db = await DbComJogo(jogo);
        var cache = new CatalogCacheFake();
        var cached = new CachedListarJogosHandler(new ListarJogosHandler(db), cache);

        var resultado = await cached.HandleAsync(CancellationToken.None);

        resultado.Should().ContainSingle(j => j.Id == jogo.Id);
        cache.ChamadasSet.Should().Be(1);
    }

    [Fact]
    public async Task Listar_SegundaChamada_DeveSerHitENaoConsultarBanco()
    {
        var jogo = Jogo.Criar("Cyber Adventure", "RPG cyberpunk", "RPG", 199.90m);
        using var db = await DbComJogo(jogo);
        var cache = new CatalogCacheFake();
        var cached = new CachedListarJogosHandler(new ListarJogosHandler(db), cache);

        await cached.HandleAsync(CancellationToken.None); // grava no cache (miss)
        db.Dispose(); // se a 2ª chamada consultar o banco, isso lança ObjectDisposedException

        var resultado = await cached.HandleAsync(CancellationToken.None);

        resultado.Should().ContainSingle(j => j.Id == jogo.Id);
        cache.ChamadasSet.Should().Be(1); // não gravou de novo: foi hit, handler interno não rodou
    }

    [Fact]
    public async Task Listar_AposRemoverChave_DeveVoltarASerMissEConsultarONovamente()
    {
        var jogo = Jogo.Criar("Cyber Adventure", "RPG cyberpunk", "RPG", 199.90m);
        using var db = await DbComJogo(jogo);
        var cache = new CatalogCacheFake();
        var cached = new CachedListarJogosHandler(new ListarJogosHandler(db), cache);

        await cached.HandleAsync(CancellationToken.None);
        await cache.RemoveAsync(CatalogCacheKeys.ListaJogosAtivos, CancellationToken.None);

        await cached.HandleAsync(CancellationToken.None);

        cache.ChamadasSet.Should().Be(2); // voltou a gravar: houve novo miss após a invalidação
    }

    // ---- CachedBuscarJogoHandler ----

    [Fact]
    public async Task Buscar_PrimeiraChamada_DeveSerMissEGravarNoCache()
    {
        var jogo = Jogo.Criar("Cyber Adventure", "RPG cyberpunk", "RPG", 199.90m);
        using var db = await DbComJogo(jogo);
        var cache = new CatalogCacheFake();
        var cached = new CachedBuscarJogoHandler(new BuscarJogoHandler(db), cache);

        var resultado = await cached.HandleAsync(jogo.Id, CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(jogo.Id);
        cache.ChamadasSet.Should().Be(1);
    }

    [Fact]
    public async Task Buscar_SegundaChamada_DeveSerHitENaoConsultarBanco()
    {
        var jogo = Jogo.Criar("Cyber Adventure", "RPG cyberpunk", "RPG", 199.90m);
        using var db = await DbComJogo(jogo);
        var cache = new CatalogCacheFake();
        var cached = new CachedBuscarJogoHandler(new BuscarJogoHandler(db), cache);

        await cached.HandleAsync(jogo.Id, CancellationToken.None);
        db.Dispose();

        var resultado = await cached.HandleAsync(jogo.Id, CancellationToken.None);

        resultado!.Id.Should().Be(jogo.Id);
        cache.ChamadasSet.Should().Be(1);
    }

    [Fact]
    public async Task Buscar_AposRemoverChave_DeveVoltarASerMissEConsultarONovamente()
    {
        var jogo = Jogo.Criar("Cyber Adventure", "RPG cyberpunk", "RPG", 199.90m);
        using var db = await DbComJogo(jogo);
        var cache = new CatalogCacheFake();
        var cached = new CachedBuscarJogoHandler(new BuscarJogoHandler(db), cache);

        await cached.HandleAsync(jogo.Id, CancellationToken.None);
        await cache.RemoveAsync(CatalogCacheKeys.Jogo(jogo.Id), CancellationToken.None);

        await cached.HandleAsync(jogo.Id, CancellationToken.None);

        cache.ChamadasSet.Should().Be(2);
    }

    [Fact]
    public async Task Buscar_QuandoNaoEncontrado_NaoDeveCachearResultadoNulo()
    {
        using var db = TestCatalogDbContext.Criar();
        var cache = new CatalogCacheFake();
        var cached = new CachedBuscarJogoHandler(new BuscarJogoHandler(db), cache);

        var resultado = await cached.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        resultado.Should().BeNull();
        cache.ChamadasSet.Should().Be(0);
    }
}
