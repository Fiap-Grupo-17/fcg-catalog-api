using FCG.CatalogAPI.Application.Loja.CatalogoEstendido;

namespace FCG.CatalogAPI.Application.Comum.Interfaces;

/// <summary>
/// Repositório do read model "catálogo expandido" (MongoDB). Implementado na
/// Infrastructure sobre <c>MongoDB.Driver</c>.
/// </summary>
public interface IGameReadModelRepository
{
    Task<GameExtendedReadModel?> GetByGameIdAsync(Guid gameId, CancellationToken ct);

    Task UpsertAsync(GameExtendedReadModel doc, CancellationToken ct);

    Task<IReadOnlyList<GameExtendedReadModel>> ListByTagAsync(string tag, CancellationToken ct);
}
