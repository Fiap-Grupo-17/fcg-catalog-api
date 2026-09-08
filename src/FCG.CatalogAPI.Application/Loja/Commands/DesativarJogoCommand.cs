using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public class DesativarJogoHandler
{
    private readonly ICatalogDbContext _db;
    private readonly ICatalogCache _cache;

    public DesativarJogoHandler(ICatalogDbContext db, ICatalogCache cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>Construtor de conveniência sem cache explícito (usado em testes existentes).</summary>
    public DesativarJogoHandler(ICatalogDbContext db) : this(db, CacheNulo.Instancia) { }

    public async Task<bool> HandleAsync(Guid id, CancellationToken ct)
    {
        var jogo = await _db.Jogos.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (jogo is null) return false;

        jogo.Desativar();
        await _db.SaveChangesAsync(ct);

        // Jogo desativado sai da listagem pública e seu detalhe individual também muda
        // (Ativo=false), então ambas as chaves precisam ser invalidadas.
        await _cache.RemoveAsync(CatalogCacheKeys.ListaJogosAtivos, ct);
        await _cache.RemoveAsync(CatalogCacheKeys.Jogo(id), ct);

        return true;
    }
}
