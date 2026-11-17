using FluentAssertions;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using FCG.CatalogAPI.Infrastructure.Testing;
using Moq;

namespace FCG.CatalogAPI.Tests.Busca;

/// <summary>
/// TC4 item 3 — Sincronização: sempre que um jogo é inserido ou editado no banco principal,
/// o índice de busca (Elasticsearch) deve ser atualizado.
/// </summary>
public class SincronizacaoIndiceBuscaTests
{
    private static readonly ICatalogCache Cache = new NoOpCatalogCache();

    private static async Task<Application.TestCatalogDbContext> DbComJogo(Jogo jogo)
    {
        var db = Application.TestCatalogDbContext.Criar();
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);
        return db;
    }

    [Fact]
    public async Task CriarJogo_DeveIndexarJogoNovo()
    {
        using var db = Application.TestCatalogDbContext.Criar();
        var busca = new Mock<IJogoSearchIndex>();
        var handler = new CriarJogoHandler(db, Cache, busca.Object);

        var result = await handler.HandleAsync(
            new CriarJogoCommand("Aventura Espacial", "desc", "Aventura", 39.90m), CancellationToken.None);

        busca.Verify(b => b.IndexarAsync(
            It.Is<Jogo>(j => j.Id == result.Id && j.Titulo == "Aventura Espacial"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarJogo_Ativo_DeveReindexarComDadosNovos()
    {
        var jogo = Jogo.Criar("Titulo Antigo", "desc", "RPG", 10m);
        using var db = await DbComJogo(jogo);
        var busca = new Mock<IJogoSearchIndex>();
        var handler = new AtualizarJogoHandler(db, Cache, busca.Object);

        await handler.HandleAsync(jogo.Id, new AtualizarJogoCommand("Titulo Novo", "desc", "RPG", 20m), CancellationToken.None);

        busca.Verify(b => b.IndexarAsync(
            It.Is<Jogo>(j => j.Id == jogo.Id && j.Titulo == "Titulo Novo" && j.Preco == 20m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarJogo_Desativado_NaoDeveVoltarParaOIndice()
    {
        var jogo = Jogo.Criar("Jogo Inativo", "desc", "RPG", 10m);
        jogo.Desativar();
        using var db = await DbComJogo(jogo);
        var busca = new Mock<IJogoSearchIndex>();
        var handler = new AtualizarJogoHandler(db, Cache, busca.Object);

        await handler.HandleAsync(jogo.Id, new AtualizarJogoCommand("Outro Titulo", "desc", "RPG", 10m), CancellationToken.None);

        busca.Verify(b => b.IndexarAsync(It.IsAny<Jogo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarJogo_Inexistente_NaoDeveTocarNoIndice()
    {
        using var db = Application.TestCatalogDbContext.Criar();
        var busca = new Mock<IJogoSearchIndex>();
        var handler = new AtualizarJogoHandler(db, Cache, busca.Object);

        var result = await handler.HandleAsync(Guid.NewGuid(), new AtualizarJogoCommand("X", "d", "g", 1m), CancellationToken.None);

        result.Should().BeNull();
        busca.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DesativarJogo_DeveRemoverDoIndice()
    {
        var jogo = Jogo.Criar("Corrida Espacial", "desc", "Corrida", 10m);
        using var db = await DbComJogo(jogo);
        var busca = new Mock<IJogoSearchIndex>();
        var handler = new DesativarJogoHandler(db, Cache, busca.Object);

        await handler.HandleAsync(jogo.Id, CancellationToken.None);

        busca.Verify(b => b.RemoverAsync(jogo.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtivarJogo_DeveVoltarAoIndice()
    {
        var jogo = Jogo.Criar("Corrida Espacial", "desc", "Corrida", 10m);
        jogo.Desativar();
        using var db = await DbComJogo(jogo);
        var busca = new Mock<IJogoSearchIndex>();
        var handler = new AtivarJogoHandler(db, Cache, busca.Object);

        await handler.HandleAsync(jogo.Id, CancellationToken.None);

        busca.Verify(b => b.IndexarAsync(It.Is<Jogo>(j => j.Id == jogo.Id && j.Ativo), It.IsAny<CancellationToken>()), Times.Once);
    }
}
