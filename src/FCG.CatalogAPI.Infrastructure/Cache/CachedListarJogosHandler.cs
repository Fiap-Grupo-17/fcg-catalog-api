using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Queries;

namespace FCG.CatalogAPI.Infrastructure.Cache;

/// <summary>
/// Decorator cache-aside sobre <see cref="ListarJogosHandler"/>. A listagem pública do
/// catálogo é lida com alta frequência e muda pouco (apenas em criação/atualização/
/// ativação/desativação de jogos), por isso é uma boa candidata a cache com TTL curto
/// (5 min) — reduz carga no Postgres sem arriscar inconsistência relevante, já que as
/// escritas invalidam a chave explicitamente (ver command handlers em Loja/Commands).
/// </summary>
public class CachedListarJogosHandler : IListarJogosHandler
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly ListarJogosHandler _handlerInterno;
    private readonly ICatalogCache _cache;

    public CachedListarJogosHandler(ListarJogosHandler handlerInterno, ICatalogCache cache)
    {
        _handlerInterno = handlerInterno;
        _cache = cache;
    }

    public async Task<List<JogoDto>> HandleAsync(CancellationToken ct)
    {
        var emCache = await _cache.GetAsync<List<JogoDto>>(CatalogCacheKeys.ListaJogosAtivos, ct);
        if (emCache is not null) return emCache;

        var jogos = await _handlerInterno.HandleAsync(ct);
        await _cache.SetAsync(CatalogCacheKeys.ListaJogosAtivos, jogos, Ttl, ct);
        return jogos;
    }
}
