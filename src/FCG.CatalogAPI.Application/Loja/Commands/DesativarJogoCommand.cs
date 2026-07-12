using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public class DesativarJogoHandler
{
    private readonly ICatalogDbContext _db;
    public DesativarJogoHandler(ICatalogDbContext db) => _db = db;

    public async Task<bool> HandleAsync(Guid id, CancellationToken ct)
    {
        var jogo = await _db.Jogos.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (jogo is null) return false;

        jogo.Desativar();
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
