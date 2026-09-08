using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.CatalogoEstendido.Queries;

public class BuscarDetalhesJogoHandler
{
    private readonly ICatalogDbContext _db;
    private readonly IGameReadModelRepository _readModelRepository;

    public BuscarDetalhesJogoHandler(ICatalogDbContext db, IGameReadModelRepository readModelRepository)
    {
        _db = db;
        _readModelRepository = readModelRepository;
    }

    /// <summary>
    /// Busca o read model estendido no Mongo. Se ainda não existir mas o jogo-base
    /// existir no Postgres, faz LAZY-SEED de um documento esqueleto (mídia/screenshots/
    /// tags/requisitos vazios, <c>Origem = "lazy-seed"</c>) para popular o Mongo sob
    /// demanda, evitando um job de backfill inicial. Retorna <c>null</c> apenas quando
    /// o jogo-base também não existe no Postgres (404 no endpoint).
    /// </summary>
    public async Task<GameExtendedReadModel?> HandleAsync(Guid gameId, CancellationToken ct)
    {
        var existente = await _readModelRepository.GetByGameIdAsync(gameId, ct);
        if (existente is not null) return existente;

        var jogo = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(j => j.Id == gameId, ct);
        if (jogo is null) return null;

        var seed = new GameExtendedReadModel
        {
            GameId = jogo.Id,
            Titulo = jogo.Titulo,
            Genero = jogo.Genero,
            Preco = jogo.Preco,
            Midia = new MidiaInfo(),
            Screenshots = new List<string>(),
            Tags = new List<string>(),
            Requisitos = new RequisitosInfo(),
            Origem = "lazy-seed"
        };

        await _readModelRepository.UpsertAsync(seed, ct);
        return seed;
    }
}
