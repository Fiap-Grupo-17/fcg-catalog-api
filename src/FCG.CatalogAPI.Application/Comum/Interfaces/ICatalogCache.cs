namespace FCG.CatalogAPI.Application.Comum.Interfaces;

/// <summary>
/// Abstração de cache distribuído (cache-aside) do catálogo. Implementada na
/// Infrastructure sobre Redis (IDistributedCache). Mantém a Application livre de
/// dependência direta de infraestrutura de cache.
/// </summary>
public interface ICatalogCache
{
    /// <summary>Lê e desserializa um valor; retorna default se ausente/expirado.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct);

    /// <summary>Serializa e grava um valor com TTL absoluto.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct);

    /// <summary>Remove uma chave (invalidação).</summary>
    Task RemoveAsync(string key, CancellationToken ct);
}
