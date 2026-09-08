namespace FCG.CatalogAPI.Application.Comum.Cache;

/// <summary>
/// Centraliza a construção das chaves de cache do catálogo. Vive na Application (e não na
/// Infrastructure) porque é consumida dos dois lados: pelos decorators de cache-aside
/// (CachedListarJogosHandler/CachedBuscarJogoHandler, na Infrastructure) e pelos command
/// handlers de escrita (CriarJogoHandler, AtualizarJogoHandler, DesativarJogoHandler,
/// AtivarJogoHandler, aqui na Application) que precisam invalidar essas chaves após
/// SaveChangesAsync. Colocá-la na Infrastructure obrigaria a Application a referenciá-la,
/// invertendo a direção de dependência da Clean Architecture (Infrastructure -> Application).
/// </summary>
public static class CatalogCacheKeys
{
    /// <summary>Chave da listagem de jogos ativos (usada em ListarJogosHandler).</summary>
    public const string ListaJogosAtivos = "catalog:jogos:list:ativos";

    /// <summary>Chave de um jogo individual por Id (usada em BuscarJogoHandler).</summary>
    public static string Jogo(Guid id) => $"catalog:jogo:{id}";
}
