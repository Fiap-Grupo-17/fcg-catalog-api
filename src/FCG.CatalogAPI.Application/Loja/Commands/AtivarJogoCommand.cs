using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public class AtivarJogoHandler
{
    private readonly ICatalogDbContext _db;
    private readonly ICatalogCache _cache;

    public AtivarJogoHandler(ICatalogDbContext db, ICatalogCache cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>Construtor de conveniência sem cache explícito (usado em testes existentes).</summary>
    public AtivarJogoHandler(ICatalogDbContext db) : this(db, CacheNulo.Instancia) { }

    public async Task<bool> HandleAsync(Guid id, CancellationToken ct)
    {
        var jogo = await _db.Jogos.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (jogo is null) return false;

        jogo.Ativar();
        await _db.SaveChangesAsync(ct);

        // Jogo reativado volta a aparecer na listagem e seu detalhe individual também
        // muda (Ativo=true), então ambas as chaves precisam ser invalidadas.
        await _cache.RemoveAsync(CatalogCacheKeys.ListaJogosAtivos, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.Jogo(id), ct);

        return true;
    }
}
