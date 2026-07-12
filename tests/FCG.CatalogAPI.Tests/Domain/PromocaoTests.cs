using FluentAssertions;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Tests.Domain;

public class PromocaoTests
{
    private static IEnumerable<Guid> JogosValidos() => new[] { Guid.NewGuid() };

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(101)]
    public void Criar_ComPercentualInvalido_DeveLancarArgumentException(decimal percentual)
    {
        Action act = () => Promocao.Criar(
            "Teste", percentual,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1), JogosValidos());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Criar_ComDataFimAnteriorAInicio_DeveLancarArgumentException()
    {
        Action act = () => Promocao.Criar(
            "Teste", 10m,
            new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            JogosValidos());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Criar_SemJogos_DeveLancarArgumentException()
    {
        Action act = () => Promocao.Criar("X", 10m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            Array.Empty<Guid>());
        act.Should().Throw<ArgumentException>().WithMessage("*ao menos um jogo*");
    }

    [Fact]
    public void Criar_ComDadosValidos_DeveCriarComAtivaTrue()
    {
        var idJogo = Guid.NewGuid();
        var promo = Promocao.Criar("Black Friday", 20m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7),
            new[] { idJogo });

        promo.Nome.Should().Be("Black Friday");
        promo.PercentualDesconto.Should().Be(20m);
        promo.Ativa.Should().BeTrue();
        promo.JogosIds.Should().ContainSingle().Which.Should().Be(idJogo);
    }

    [Fact]
    public void EstaVigenteEm_QuandoDentroDoPeriodo_DeveRetornarTrue()
    {
        var promo = Promocao.Criar("Teste", 20m,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            JogosValidos());

        promo.EstaVigenteEm(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc))
             .Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteEm_QuandoEncerrada_DeveRetornarFalse()
    {
        var promo = Promocao.Criar("X", 10m,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1),
            JogosValidos());
        promo.Encerrar();
        promo.EstaVigenteEm(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void AplicarDesconto_Com20Porcento_DeveRetornar80()
    {
        var promo = Promocao.Criar("X", 20m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1), JogosValidos());
        promo.AplicarDesconto(100m).Should().Be(80.00m);
    }

    [Fact]
    public void ContemJogo_DeveRetornarTrueParaIdsVinculados()
    {
        var idJogo = Guid.NewGuid();
        var promo = Promocao.Criar("X", 10m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            new[] { idJogo });
        promo.ContemJogo(idJogo).Should().BeTrue();
        promo.ContemJogo(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Encerrar_DeveDefinirAtivaComoFalse()
    {
        var promo = Promocao.Criar("X", 10m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1), JogosValidos());
        promo.Ativa.Should().BeTrue();

        promo.Encerrar();

        promo.Ativa.Should().BeFalse();
    }
}
