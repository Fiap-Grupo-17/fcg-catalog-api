using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using FCG.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public record ReprocessarPedidoResult(Guid PedidoId, string Mensagem);

public class ReprocessarPedidoHandler
{
    private readonly ICatalogDbContext _db;
    private readonly IEventBus _eventBus;

    public ReprocessarPedidoHandler(ICatalogDbContext db, IEventBus eventBus)
    {
        _db = db;
        _eventBus = eventBus;
    }

    public async Task<ReprocessarPedidoResult> HandleAsync(
        Guid pedidoId,
        Guid usuarioId,
        bool isAdmin,
        CancellationToken ct)
    {
        var pedido = await _db.Pedidos
            .FirstOrDefaultAsync(p => p.Id == pedidoId, ct)
            ?? throw new KeyNotFoundException($"Pedido {pedidoId} não encontrado.");

        // Somente o dono do pedido ou um administrador pode reprocessar
        if (!isAdmin && pedido.UsuarioId != usuarioId)
            throw new UnauthorizedAccessException("Acesso negado: pedido pertence a outro usuário.");

        if (pedido.Status != StatusPedido.Pendente)
            throw new InvalidOperationException(
                $"Apenas pedidos com status Pendente podem ser reprocessados. Status atual: {pedido.Status}.");

        // Re-publica o evento para a PaymentsAPI consumir
        await _eventBus.PublicarAsync(new OrderPlacedEvent(
            OrderId: pedido.Id,
            UserId: pedido.UsuarioId,
            GameId: pedido.JogoId,
            GameName: pedido.JogoTitulo,
            Price: pedido.Valor,
            PlacedAt: pedido.CriadoEm), ct);

        return new ReprocessarPedidoResult(
            pedido.Id,
            "Pedido enviado para reprocessamento. Aguarde alguns instantes e consulte o status novamente.");
    }
}
