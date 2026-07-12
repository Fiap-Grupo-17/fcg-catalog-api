using FluentAssertions;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Biblioteca.Entidades;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Tests.Application;

public class IniciarAquisicaoHandlerTests
{
    private static IniciarAquisicaoHandler CriarHandler(ICatalogDbContext db)
        => new(db, new NoOpEventBusForUnitTests());

    // ── helpers ────────────────────────────────────────────────────────────

    private static async Task<(TestCatalogDbContext db, Jogo jogo)> DbComJogoAtivoAsync()
    {
        var db = TestCatalogDbContext.Criar();
        var jogo = Jogo.Criar("Jogo Teste", "desc", "RPG", 59.90m);
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);
        return (db, jogo);
    }

    // ── testes ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_JogoNaoExiste_DeveLancarKeyNotFoundException()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = CriarHandler(db);
        var cmd = new IniciarAquisicaoCommand(Guid.NewGuid(), Guid.NewGuid());

        Func<Task> act = () => handler.HandleAsync(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*não encontrado*");
    }

    [Fact]
    public async Task HandleAsync_JogoInativo_DeveLancarKeyNotFoundException()
    {
        using var db = TestCatalogDbContext.Criar();
        var jogo = Jogo.Criar("Jogo Inativo", "desc", "RPG", 30m);
        jogo.Desativar();
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = CriarHandler(db);
        var cmd = new IniciarAquisicaoCommand(Guid.NewGuid(), jogo.Id);

        Func<Task> act = () => handler.HandleAsync(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_UsuarioJaPossui_DeveLancarInvalidOperationException()
    {
        var (db, jogo) = await DbComJogoAtivoAsync();
        var usuarioId = Guid.NewGuid();

        // Simula que o usuário já possui o jogo na biblioteca
        var item = ItemBiblioteca.Criar(usuarioId, jogo.Id, jogo.Titulo, Guid.NewGuid());
        db.ItensBiblioteca.Add(item);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = CriarHandler(db);
        var cmd = new IniciarAquisicaoCommand(usuarioId, jogo.Id);

        Func<Task> act = () => handler.HandleAsync(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já possui*");
    }

    [Fact]
    public async Task HandleAsync_PedidoPendente_DeveLancarInvalidOperationException()
    {
        var (db, jogo) = await DbComJogoAtivoAsync();
        var usuarioId = Guid.NewGuid();

        // Simula pedido já pendente
        var pedido = Pedido.Criar(usuarioId, jogo.Id, jogo.Titulo, jogo.Preco);
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = CriarHandler(db);
        var cmd = new IniciarAquisicaoCommand(usuarioId, jogo.Id);

        Func<Task> act = () => handler.HandleAsync(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pedido pendente*");
    }

    [Fact]
    public async Task HandleAsync_HappyPath_DeveCriarPedidoComStatusPendente()
    {
        var (db, jogo) = await DbComJogoAtivoAsync();
        var usuarioId = Guid.NewGuid();
        var handler = CriarHandler(db);
        var cmd = new IniciarAquisicaoCommand(usuarioId, jogo.Id);

        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.PedidoId.Should().NotBeEmpty();
        result.Mensagem.Should().NotBeNullOrWhiteSpace();

        var pedidoSalvo = db.Pedidos.Single(p => p.Id == result.PedidoId);
        pedidoSalvo.UsuarioId.Should().Be(usuarioId);
        pedidoSalvo.JogoId.Should().Be(jogo.Id);
        pedidoSalvo.Status.Should().Be(StatusPedido.Pendente);
    }
}

/// <summary>IEventBus sem efeito colateral para testes unitários de handlers.</summary>
file sealed class NoOpEventBusForUnitTests : FCG.CatalogAPI.Application.Comum.Interfaces.IEventBus
{
    public Task PublicarAsync<T>(T evento, CancellationToken ct = default) where T : class
        => Task.CompletedTask;
}
