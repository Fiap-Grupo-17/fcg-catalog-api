using FCG.CatalogAPI.Application.Comum.Busca;
using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public record CriarJogoCommand(string Titulo, string Descricao, string Genero, decimal Preco);

public record CriarJogoResult(Guid Id, string Titulo, decimal Preco);

public class CriarJogoHandler
{
    private readonly ICatalogDbContext _db;
    private readonly ICatalogCache _cache;
    private readonly IJogoSearchIndex _busca;

    public CriarJogoHandler(ICatalogDbContext db, ICatalogCache cache, IJogoSearchIndex busca)
    {
        _db = db;
        _cache = cache;
        _busca = busca;
    }

    /// <summary>Construtor de conveniência sem índice de busca explícito (usado em testes existentes).</summary>
    public CriarJogoHandler(ICatalogDbContext db, ICatalogCache cache) : this(db, cache, IndiceBuscaNulo.Instancia) { }

    /// <summary>Construtor de conveniência sem cache explícito (usado em testes existentes).</summary>
    public CriarJogoHandler(ICatalogDbContext db) : this(db, CacheNulo.Instancia) { }

    public async Task<CriarJogoResult> HandleAsync(CriarJogoCommand cmd, CancellationToken ct)
    {
        var jogo = Jogo.Criar(cmd.Titulo, cmd.Descricao, cmd.Genero, cmd.Preco);
        _db.Jogos.Add(jogo);
        await _db.SaveChangesAsync(ct);

        // Invalida a listagem cacheada: um jogo novo deve aparecer na próxima leitura,
        // não há chave individual para invalidar pois o jogo ainda não existia em cache.
        await _cache.RemoveAsync(CatalogCacheKeys.ListaJogosAtivos, ct);

        // Sincroniza o índice de busca (Elasticsearch): o jogo novo já aparece no /search.
        await _busca.IndexarAsync(jogo, ct);

        return new CriarJogoResult(jogo.Id, jogo.Titulo, jogo.Preco);
    }
}
