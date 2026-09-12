using System.Collections.Concurrent;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.CatalogoEstendido;

namespace FCG.CatalogAPI.Infrastructure.Testing;

/// <summary>
/// Fakes in-memory usados no ambiente "Testing", para que a suíte de integração
/// existente (EF InMemory) não passe a exigir containers Redis/Mongo.
/// </summary>

/// <summary>Cache que sempre erra (miss) — o fluxo cai para o Postgres/InMemory, preservando o comportamento legado.</summary>
public sealed class NoOpCatalogCache : ICatalogCache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct) => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;
    public Task RemoveAsync(string key, CancellationToken ct) => Task.CompletedTask;
}

/// <summary>Idempotência in-memory: registra a primeira ocorrência de (consumer, messageId).</summary>
public sealed class InMemoryProcessedEventStore : IProcessedEventStore
{
    private readonly ConcurrentDictionary<string, byte> _seen = new();

    public Task<bool> TryRegisterAsync(Guid messageId, string consumer, string? businessKey, string eventType, CancellationToken ct)
    {
        var first = _seen.TryAdd($"{consumer}|{messageId}", 1);
        return Task.FromResult(first);
    }
}

/// <summary>Read model in-memory.</summary>
public sealed class InMemoryGameReadModelRepository : IGameReadModelRepository
{
    private readonly ConcurrentDictionary<Guid, GameExtendedReadModel> _store = new();

    public Task<GameExtendedReadModel?> GetByGameIdAsync(Guid gameId, CancellationToken ct)
        => Task.FromResult(_store.TryGetValue(gameId, out var doc) ? doc : null);

    public Task UpsertAsync(GameExtendedReadModel doc, CancellationToken ct)
    {
        doc.AtualizadoEm = DateTime.UtcNow;
        _store[doc.GameId] = doc;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GameExtendedReadModel>> ListByTagAsync(string tag, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<GameExtendedReadModel>>(
            _store.Values.Where(d => d.Tags.Contains(tag)).ToList());
}
