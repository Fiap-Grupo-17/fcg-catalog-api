using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using FCG.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public record IniciarAquisicaoCommand(Guid UsuarioId, Guid JogoId);

public record IniciarAquisicaoResult(Guid PedidoId, string Mensagem);

public class IniciarAquisicaoHandler
{
    private readonly ICatalogDbContext _db;
    private readonly IEventBus _eventBus;

    public IniciarAquisicaoHandler(ICatalogDbContext db, IEventBus eventBus)
    {
        _db = db;
        _eventBus = eventBus;
    }

    public async Task<IniciarAquisicaoResult> HandleAsync(IniciarAquisicaoCommand cmd, CancellationToken ct)
    {
        var jogo = await _db.Jogos
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == cmd.JogoId && j.Ativo, ct)
            ?? throw new KeyNotFoundException($"Jogo {cmd.JogoId} não encontrado ou inativo.");

        // Verifica se já possui o jogo
        var jaAdquiriu = await _db.ItensBiblioteca
            .AnyAsync(i => i.UsuarioId == cmd.UsuarioId && i.JogoId == cmd.JogoId, ct);

        if (jaAdquiriu)
            throw new InvalidOperationException("Usuário já possui este jogo na biblioteca.");

        // Verifica se já existe pedido pendente
        var pedidoPendente = await _db.Pedidos
            .AnyAsync(p => p.UsuarioId == cmd.UsuarioId && p.JogoId == cmd.JogoId
                           && p.Status == StatusPedido.Pendente, ct);

        if (pedidoPendente)
            throw new InvalidOperationException("Já existe um pedido pendente para este jogo.");

        var pedido = Pedido.Criar(cmd.UsuarioId, cmd.JogoId, jogo.Titulo, jogo.Preco);
        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync(ct);

        // Publica evento para PaymentsAPI processar (async — 202 Accepted)
        await _eventBus.PublicarAsync(new OrderPlacedEvent(
            OrderId: pedido.Id,
            UserId: cmd.UsuarioId,
            GameId: jogo.Id,
            GameName: jogo.Titulo,
            Price: jogo.Preco,
            PlacedAt: pedido.CriadoEm), ct);

        return new IniciarAquisicaoResult(pedido.Id,
            "Pedido criado com sucesso. O pagamento será processado em instantes.");
    }
}
