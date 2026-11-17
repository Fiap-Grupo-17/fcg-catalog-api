using FluentAssertions;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Busca;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using FCG.CatalogAPI.Infrastructure.Testing;
using Moq;

namespace FCG.CatalogAPI.Tests.Busca;

public class BuscarJogosPorTextoHandlerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TermoVazio_DeveLancarArgumentException(string? termo)
    {
        var handler = new BuscarJogosPorTextoHandler(Mock.Of<IJogoSearchIndex>());

        var act = () => handler.HandleAsync(termo, null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null, BuscarJogosPorTextoHandler.TamanhoPadrao)]
    [InlineData(0, 1)]
    [InlineData(500, BuscarJogosPorTextoHandler.TamanhoMaximo)]
    [InlineData(5, 5)]
    public async Task Tamanho_DeveSerLimitado(int? pedido, int esperado)
    {
        var indice = new Mock<IJogoSearchIndex>();
        indice.Setup(i => i.BuscarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<JogoBuscaDto>());
        var handler = new BuscarJogosPorTextoHandler(indice.Object);

        await handler.HandleAsync("  aventura  ", pedido, CancellationToken.None);

        indice.Verify(i => i.BuscarAsync("aventura", esperado, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Comportamento esperado da busca (validado no índice in-memory usado nos testes) ──

    private static async Task<InMemoryJogoSearchIndex> IndiceCom(params Jogo[] jogos)
    {
        var indice = new InMemoryJogoSearchIndex();
        foreach (var j in jogos) await indice.IndexarAsync(j, CancellationToken.None);
        return indice;
    }

    [Fact]
    public async Task ErroDeDigitacao_DeveEncontrar_Fuzzy()
    {
        var indice = await IndiceCom(Jogo.Criar("Aventura Espacial", "desc", "Aventura", 10m));

        var resultado = await indice.BuscarAsync("aventrua", 10, CancellationToken.None);

        resultado.Should().ContainSingle(r => r.Titulo == "Aventura Espacial");
    }

    [Fact]
    public async Task VariosTermos_DeveOrdenarPorRelevancia()
    {
        var indice = await IndiceCom(
            Jogo.Criar("Corrida Espacial", "desc", "Corrida", 10m),
            Jogo.Criar("Aventura Espacial", "desc", "Aventura", 10m),
            Jogo.Criar("Aventura na Floresta", "desc", "Aventura", 10m));

        var resultado = await indice.BuscarAsync("aventura espacial", 10, CancellationToken.None);

        resultado.First().Titulo.Should().Be("Aventura Espacial");
        resultado.Select(r => r.Score).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task JogoRemovido_NaoDeveAparecer()
    {
        var jogo = Jogo.Criar("Aventura Espacial", "desc", "Aventura", 10m);
        var indice = await IndiceCom(jogo);

        await indice.RemoverAsync(jogo.Id, CancellationToken.None);

        (await indice.BuscarAsync("aventura", 10, CancellationToken.None)).Should().BeEmpty();
    }
}
