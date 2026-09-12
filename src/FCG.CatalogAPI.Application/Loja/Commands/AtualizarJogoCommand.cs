using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public record AtualizarJogoCommand(string Titulo, string Descricao, string Genero, decimal Preco);
public record AtualizarJogoResult(Guid Id, string Titulo, decimal Preco);

public class AtualizarJogoHandler
{
    private readonly ICatalogDbContext _db;
    private readonly ICatalogCache _cache;

    public AtualizarJogoHandler(ICatalogDbContext db, ICatalogCache cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>Construtor de conveniência sem cache explícito (usado em testes existentes).</summary>
    public AtualizarJogoHandler(ICatalogDbContext db) : this(db, CacheNulo.Instancia) { }

    public async Task<AtualizarJogoResult?> HandleAsync(Guid id, AtualizarJogoCommand cmd, CancellationToken ct)
    {
        var jogo = await _db.Jogos.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (jogo is null) return null;

        jogo.Atualizar(cmd.Titulo, cmd.Descricao, cmd.Genero, cmd.Preco);
        await _db.SaveChangesAsync(ct);

        // Título/descrição/gênero/preço mudaram: invalida tanto a listagem quanto o
        // item individual para não servir dados obsoletos até o TTL expirar.
        await _cache.RemoveAsync(CatalogCacheKeys.ListaJogosAtivos, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.Jogo(id), ct);

        return new AtualizarJogoResult(jogo.Id, jogo.Titulo, jogo.Preco);
    }
}
