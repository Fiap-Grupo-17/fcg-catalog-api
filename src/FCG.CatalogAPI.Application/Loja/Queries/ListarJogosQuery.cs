using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Queries;

public record JogoDto(Guid Id, string Titulo, string Descricao, string Genero, decimal Preco);

public class ListarJogosHandler : IListarJogosHandler
{
    private readonly ICatalogDbContext _db;

    public ListarJogosHandler(ICatalogDbContext db) => _db = db;

    public async Task<List<JogoDto>> HandleAsync(CancellationToken ct)
        => await _db.Jogos
            .AsNoTracking()
            .Where(j => j.Ativo)
            .OrderBy(j => j.Titulo)
            .Select(j => new JogoDto(j.Id, j.Titulo, j.Descricao, j.Genero, j.Preco))
            .ToListAsync(ct);
}
