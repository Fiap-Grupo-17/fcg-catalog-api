using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.CatalogoEstendido.Commands;

/// <summary>
/// Comando de upsert dos metadados ricos ("catálogo estendido") de um jogo já
/// cadastrado no PostgreSQL. O <c>GameId</c> vem da rota, não do corpo da requisição —
/// este comando nunca cria um jogo-base, apenas enriquece um jogo existente.
/// </summary>
public record UpsertDetalhesJogoCommand(
    MidiaInfo? Midia,
    List<string>? Screenshots,
    List<string>? Tags,
    RequisitosInfo? Requisitos);

public class UpsertDetalhesJogoHandler
{
    private readonly ICatalogDbContext _db;
    private readonly IGameReadModelRepository _readModelRepository;

    public UpsertDetalhesJogoHandler(ICatalogDbContext db, IGameReadModelRepository readModelRepository)
    {
        _db = db;
        _readModelRepository = readModelRepository;
    }

    /// <summary>
    /// Valida a existência do jogo-base no Postgres (system-of-record) antes de gravar
    /// no Mongo. Retorna <c>null</c> quando o jogo-base não existe — o endpoint traduz
    /// isso para 404, evitando criar um read model "órfão" sem jogo correspondente.
    /// </summary>
    public async Task<GameExtendedReadModel?> HandleAsync(Guid gameId, UpsertDetalhesJogoCommand cmd, CancellationToken ct)
    {
        var jogo = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(j => j.Id == gameId, ct);
        if (jogo is null) return null;

        var documento = new GameExtendedReadModel
        {
            GameId = jogo.Id,
            Titulo = jogo.Titulo,
            Genero = jogo.Genero,
            Preco = jogo.Preco,
            Midia = cmd.Midia ?? new MidiaInfo(),
            Screenshots = cmd.Screenshots ?? new List<string>(),
            Tags = cmd.Tags ?? new List<string>(),
            Requisitos = cmd.Requisitos ?? new RequisitosInfo(),
            Origem = "admin"
        };

        await _readModelRepository.UpsertAsync(documento, ct);
        return documento;
    }
}
