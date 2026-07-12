using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Queries;

public class BuscarJogoHandler
{
    private readonly ICatalogDbContext _db;
    public BuscarJogoHandler(ICatalogDbContext db) => _db = db;

    public async Task<JogoDto?> HandleAsync(Guid id, CancellationToken ct)
        => await _db.Jogos
            .AsNoTracking()
            .Where(j => j.Id == id)
            .Select(j => new JogoDto(j.Id, j.Titulo, j.Descricao, j.Genero, j.Preco))
            .FirstOrDefaultAsync(ct);
}
