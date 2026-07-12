using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public class AtivarJogoHandler
{
    private readonly ICatalogDbContext _db;
    public AtivarJogoHandler(ICatalogDbContext db) => _db = db;

    public async Task<bool> HandleAsync(Guid id, CancellationToken ct)
    {
        var jogo = await _db.Jogos.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (jogo is null) return false;

        jogo.Ativar();
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
