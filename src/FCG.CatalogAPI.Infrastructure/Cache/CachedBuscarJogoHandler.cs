using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Queries;

namespace FCG.CatalogAPI.Infrastructure.Cache;

/// <summary>
/// Decorator cache-aside sobre <see cref="BuscarJogoHandler"/>. TTL de 10 min (maior que
/// o da listagem, pois a leitura por Id é ainda mais estável). Resultados "não encontrado"
/// (null) propositalmente NÃO são cacheados: cachear negativos criaria uma janela em que
/// um jogo recém-criado apareceria como 404 até o TTL expirar, o que é pior do que o
/// custo (baixo) de repetir a consulta de miss no banco.
/// </summary>
public class CachedBuscarJogoHandler : IBuscarJogoHandler
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private readonly BuscarJogoHandler _handlerInterno;
    private readonly ICatalogCache _cache;

    public CachedBuscarJogoHandler(BuscarJogoHandler handlerInterno, ICatalogCache cache)
    {
        _handlerInterno = handlerInterno;
        _cache = cache;
    }

    public async Task<JogoDto?> HandleAsync(Guid id, CancellationToken ct)
    {
        var chave = CatalogCacheKeys.Jogo(id);

        var emCache = await _cache.GetAsync<JogoDto>(chave, ct);
        if (emCache is not null) return emCache;

        var jogo = await _handlerInterno.HandleAsync(id, ct);
        if (jogo is not null) await _cache.SetAsync(chave, jogo, Ttl, ct);
        return jogo;
    }
}
