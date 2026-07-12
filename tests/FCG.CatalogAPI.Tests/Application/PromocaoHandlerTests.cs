using FluentAssertions;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Application.Loja.Queries;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Tests.Application;

public class CriarPromocaoHandlerTests
{
    [Fact]
    public async Task HandleAsync_ComJogosAtivos_DevePersistirERetornarResult()
    {
        using var db = TestCatalogDbContext.Criar();
        var jogo = Jogo.Criar("Cyber RPG", "desc", "RPG", 100m);
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CriarPromocaoHandler(db);
        var result = await handler.HandleAsync(new CriarPromocaoCommand(
            "Black Friday", 20m,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(5),
            new List<Guid> { jogo.Id }),
            CancellationToken.None);

        result.Nome.Should().Be("Black Friday");
        result.PercentualDesconto.Should().Be(20m);
        result.Ativa.Should().BeTrue();
        result.JogosIds.Should().ContainSingle().Which.Should().Be(jogo.Id);
        db.Promocoes.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_ComJogoInexistente_DeveLancarArgumentException()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new CriarPromocaoHandler(db);

        Func<Task> act = () => handler.HandleAsync(new CriarPromocaoCommand(
            "Promo", 10m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            new List<Guid> { Guid.NewGuid() }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*inativos*");
    }

    [Fact]
    public async Task HandleAsync_ComJogoInativo_DeveLancarArgumentException()
    {
        using var db = TestCatalogDbContext.Criar();
        var jogo = Jogo.Criar("RPG", "desc", "RPG", 100m);
        jogo.Desativar();
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CriarPromocaoHandler(db);

        Func<Task> act = () => handler.HandleAsync(new CriarPromocaoCommand(
            "Promo", 10m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            new List<Guid> { jogo.Id }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HandleAsync_ComPercentualInvalido_DeveLancarArgumentException()
    {
        using var db = TestCatalogDbContext.Criar();
        var jogo = Jogo.Criar("RPG", "desc", "RPG", 100m);
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CriarPromocaoHandler(db);

        Func<Task> act = () => handler.HandleAsync(new CriarPromocaoCommand(
            "Promo", 0m, // inválido
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            new List<Guid> { jogo.Id }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}

public class AtualizarPromocaoHandlerTests
{
    private static async Task<(TestCatalogDbContext db, Jogo jogo, Promocao promo)> Setup()
    {
        var db = TestCatalogDbContext.Criar();
        var jogo = Jogo.Criar("RPG", "desc", "RPG", 100m);
        db.Jogos.Add(jogo);

        var promo = Promocao.Criar("Promo Original", 10m,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(7),
            new[] { jogo.Id });
        db.Promocoes.Add(promo);
        await db.SaveChangesAsync(CancellationToken.None);
        return (db, jogo, promo);
    }

    [Fact]
    public async Task HandleAsync_IdInexistente_DeveRetornarNull()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new AtualizarPromocaoHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(),
            new AtualizarPromocaoCommand("X", 10m,
                DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
                new List<Guid> { Guid.NewGuid() }),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ComDadosValidos_DeveAtualizarERetornar()
    {
        var (db, jogo, promo) = await Setup();
        using (db)
        {
            var handler = new AtualizarPromocaoHandler(db);
            var result = await handler.HandleAsync(promo.Id,
                new AtualizarPromocaoCommand("Promo Atualizada", 25m,
                    DateTime.UtcNow, DateTime.UtcNow.AddDays(10),
                    new List<Guid> { jogo.Id }),
                CancellationToken.None);

            result.Should().NotBeNull();
            result!.Nome.Should().Be("Promo Atualizada");
            result.PercentualDesconto.Should().Be(25m);
        }
    }
}

public class EncerrarPromocaoHandlerTests
{
    [Fact]
    public async Task HandleAsync_IdInexistente_DeveRetornarFalse()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new EncerrarPromocaoHandler(db);

        var ok = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_PromocaoAtiva_DeveEncerrarERetornarTrue()
    {
        using var db = TestCatalogDbContext.Criar();
        var jogo = Jogo.Criar("RPG", "desc", "RPG", 100m);
        db.Jogos.Add(jogo);
        var promo = Promocao.Criar("Promo", 10m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(5), new[] { jogo.Id });
        db.Promocoes.Add(promo);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new EncerrarPromocaoHandler(db);
        var ok = await handler.HandleAsync(promo.Id, CancellationToken.None);

        ok.Should().BeTrue();
        db.Promocoes.Single(p => p.Id == promo.Id).Ativa.Should().BeFalse();
    }
}

public class ListarPromocoesHandlerTests
{
    [Fact]
    public async Task HandleAsync_SemFiltros_DeveRetornarTodas()
    {
        using var db = TestCatalogDbContext.Criar();
        var jogo = Jogo.Criar("RPG", "desc", "RPG", 100m);
        db.Jogos.Add(jogo);
        db.Promocoes.Add(Promocao.Criar("P1", 10m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new[] { jogo.Id }));
        db.Promocoes.Add(Promocao.Criar("P2", 20m, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), new[] { jogo.Id }));
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ListarPromocoesHandler(db);
        var result = await handler.HandleAsync(null, null, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_FiltroApenasAtivas_DeveExcluirEncerradas()
    {
        using var db = TestCatalogDbContext.Criar();
        var jogo = Jogo.Criar("RPG", "desc", "RPG", 100m);
        db.Jogos.Add(jogo);
        var ativa = Promocao.Criar("Ativa", 10m, DateTime.UtcNow, DateTime.UtcNow.AddDays(3), new[] { jogo.Id });
        var encerrada = Promocao.Criar("Encerrada", 15m, DateTime.UtcNow, DateTime.UtcNow.AddDays(3), new[] { jogo.Id });
        encerrada.Encerrar();
        db.Promocoes.AddRange(ativa, encerrada);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ListarPromocoesHandler(db);
        var result = await handler.HandleAsync(true, null, CancellationToken.None);

        result.Should().ContainSingle().Which.Nome.Should().Be("Ativa");
    }
}
