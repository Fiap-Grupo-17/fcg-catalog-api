using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Queries;

public record PedidoDto(Guid Id, Guid UsuarioId, Guid JogoId, string JogoTitulo,
    decimal Valor, string Status, string? TransacaoId, string? MotivoRejeicao,
    DateTime CriadoEm, DateTime? ProcessadoEm);

public class BuscarPedidoHandler
{
    private readonly ICatalogDbContext _db;
    public BuscarPedidoHandler(ICatalogDbContext db) => _db = db;

    public async Task<PedidoDto?> HandleAsync(Guid id, CancellationToken ct)
        => await _db.Pedidos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PedidoDto(
                p.Id, p.UsuarioId, p.JogoId, p.JogoTitulo,
                p.Valor, p.Status.ToString(), p.TransacaoId, p.MotivoRejeicao,
                p.CriadoEm, p.ProcessadoEm))
            .FirstOrDefaultAsync(ct);
}

public record ListarPedidosResult(List<PedidoDto> Itens, int Total, int Pagina, int TamanhoPagina);

public class ListarPedidosHandler
{
    private readonly ICatalogDbContext _db;
    public ListarPedidosHandler(ICatalogDbContext db) => _db = db;

    public async Task<ListarPedidosResult> HandleAsync(
        Guid? usuarioId,
        string? status,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct)
    {
        var query = _db.Pedidos.AsNoTracking().AsQueryable();

        if (usuarioId.HasValue)
            query = query.Where(p => p.UsuarioId == usuarioId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<StatusPedido>(status, ignoreCase: true, out var statusEnum))
            query = query.Where(p => p.Status == statusEnum);

        var total = await query.CountAsync(ct);

        var itens = await query
            .OrderByDescending(p => p.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(p => new PedidoDto(
                p.Id, p.UsuarioId, p.JogoId, p.JogoTitulo,
                p.Valor, p.Status.ToString(), p.TransacaoId, p.MotivoRejeicao,
                p.CriadoEm, p.ProcessadoEm))
            .ToListAsync(ct);

        return new ListarPedidosResult(itens, total, pagina, tamanhoPagina);
    }
}
