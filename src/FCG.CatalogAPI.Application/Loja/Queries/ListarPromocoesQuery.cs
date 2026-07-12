using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Commands;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Queries;

public class ListarPromocoesHandler
{
    private readonly ICatalogDbContext _db;
    public ListarPromocoesHandler(ICatalogDbContext db) => _db = db;

    /// <summary>
    /// Lista promoções com filtros opcionais.
    /// </summary>
    /// <param name="apenasAtivas">Quando true, retorna somente promoções com Ativa=true.</param>
    /// <param name="apenasVigentes">Quando true, retorna somente promoções vigentes no momento atual.</param>
    public async Task<List<PromocaoResult>> HandleAsync(
        bool? apenasAtivas, bool? apenasVigentes, CancellationToken ct)
    {
        var query = _db.Promocoes.AsNoTracking();

        if (apenasAtivas == true)
            query = query.Where(p => p.Ativa);

        var lista = await query.OrderByDescending(p => p.CriadaEm).ToListAsync(ct);

        if (apenasVigentes == true)
        {
            var agora = DateTime.UtcNow;
            lista = lista.Where(p => p.EstaVigenteEm(agora)).ToList();
        }

        return lista.Select(p => new PromocaoResult(
            p.Id, p.Nome, p.PercentualDesconto,
            p.Inicio, p.Fim, p.Ativa, p.JogosIds)).ToList();
    }
}
