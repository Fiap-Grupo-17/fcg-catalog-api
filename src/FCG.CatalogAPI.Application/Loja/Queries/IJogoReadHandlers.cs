namespace FCG.CatalogAPI.Application.Loja.Queries;

/// <summary>
/// Abstração do caso de uso de listagem de jogos ativos. Permite que a Infrastructure
/// forneça um decorator de cache-aside (<c>CachedListarJogosHandler</c>) sem que a
/// Application/API dependam de Redis diretamente.
/// </summary>
public interface IListarJogosHandler
{
    Task<List<JogoDto>> HandleAsync(CancellationToken ct);
}

/// <summary>
/// Abstração do caso de uso de busca de um jogo por Id. Permite decorator de
/// cache-aside (<c>CachedBuscarJogoHandler</c>) na Infrastructure.
/// </summary>
public interface IBuscarJogoHandler
{
    Task<JogoDto?> HandleAsync(Guid id, CancellationToken ct);
}
