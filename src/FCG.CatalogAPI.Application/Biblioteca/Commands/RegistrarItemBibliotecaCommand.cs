using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Biblioteca.Entidades;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FCG.CatalogAPI.Application.Biblioteca.Commands;

public record RegistrarItemBibliotecaCommand(
    Guid OrderId, Guid UserId, Guid GameId, string GameName,
    string Status, string? TransactionId, string? MotivoRejeicao);

public class RegistrarItemBibliotecaHandler
{
    private readonly ICatalogDbContext _db;
    private readonly ILogger<RegistrarItemBibliotecaHandler> _logger;

    public RegistrarItemBibliotecaHandler(ICatalogDbContext db, ILogger<RegistrarItemBibliotecaHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(RegistrarItemBibliotecaCommand cmd, CancellationToken ct)
    {
        var pedido = await _db.Pedidos
            .FirstOrDefaultAsync(p => p.Id == cmd.OrderId, ct);

        if (pedido is null)
        {
            _logger.LogWarning("[BIBLIOTECA] Pedido {OrderId} não encontrado para atualização.", cmd.OrderId);
            return;
        }

        if (cmd.Status == "Approved")
        {
            pedido.MarcarAprovado(cmd.TransactionId!);

            var item = ItemBiblioteca.Criar(cmd.UserId, cmd.GameId, cmd.GameName, cmd.OrderId);
            _db.ItensBiblioteca.Add(item);

            _logger.LogInformation(
                "[BIBLIOTECA] 📚 Jogo adicionado à biblioteca | UserId: {UserId} | Jogo: {GameName} | PedidoId: {OrderId}",
                cmd.UserId, cmd.GameName, cmd.OrderId);
        }
        else
        {
            pedido.MarcarRejeitado(cmd.MotivoRejeicao ?? "Pagamento recusado.");
            _logger.LogWarning(
                "[BIBLIOTECA] ❌ Pedido recusado | PedidoId: {OrderId} | Motivo: {Motivo}",
                cmd.OrderId, cmd.MotivoRejeicao);
        }

        await _db.SaveChangesAsync(ct);
    }
}
