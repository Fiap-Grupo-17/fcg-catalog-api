using FluentAssertions;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Tests.Domain;

public class JogoTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarJogo()
    {
        var jogo = Jogo.Criar("Cyber Adventure", "Um RPG futurista", "RPG", 199.90m);

        jogo.Titulo.Should().Be("Cyber Adventure");
        jogo.Descricao.Should().Be("Um RPG futurista");
        jogo.Genero.Should().Be("RPG");
        jogo.Preco.Should().Be(199.90m);
        jogo.Ativo.Should().BeTrue();
        jogo.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_ComTituloVazio_DeveLancarArgumentException(string titulo)
    {
        Action act = () => Jogo.Criar(titulo, "desc", "RPG", 100m);
        act.Should().Throw<ArgumentException>().WithMessage("*Título*");
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public void Criar_ComPrecoNegativo_DeveLancarArgumentException(decimal preco)
    {
        Action act = () => Jogo.Criar("Título Válido", "desc", "Ação", preco);
        act.Should().Throw<ArgumentException>().WithMessage("*negativo*");
    }

    [Fact]
    public void Criar_ComPrecoZero_DevePermitir()
    {
        var jogo = Jogo.Criar("Jogo Gratuito", "desc", "Free", 0m);
        jogo.Preco.Should().Be(0m);
    }

    [Fact]
    public void Atualizar_ComDadosValidos_DeveAlterarPropriedades()
    {
        var jogo = Jogo.Criar("Título Original", "Desc Original", "RPG", 50m);

        jogo.Atualizar("Título Novo", "Desc Nova", "Ação", 99m);

        jogo.Titulo.Should().Be("Título Novo");
        jogo.Descricao.Should().Be("Desc Nova");
        jogo.Genero.Should().Be("Ação");
        jogo.Preco.Should().Be(99m);
    }

    [Fact]
    public void Atualizar_ComTituloVazio_DeveLancarArgumentException()
    {
        var jogo = Jogo.Criar("Título", "desc", "RPG", 50m);
        Action act = () => jogo.Atualizar("", "desc nova", "RPG", 50m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Desativar_JogoAtivo_DeveDefinirAtivoComoFalso()
    {
        var jogo = Jogo.Criar("Título", "desc", "RPG", 50m);
        jogo.Ativo.Should().BeTrue();

        jogo.Desativar();

        jogo.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Ativar_JogoDesativado_DeveDefinirAtivoComoTrue()
    {
        var jogo = Jogo.Criar("Título", "desc", "RPG", 50m);
        jogo.Desativar();
        jogo.Ativo.Should().BeFalse();

        jogo.Ativar();

        jogo.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Desativar_EntaoAtivar_DeveCiclarStatusCorretamente()
    {
        var jogo = Jogo.Criar("Título", "desc", "RPG", 50m);

        jogo.Desativar();
        jogo.Ativo.Should().BeFalse();

        jogo.Ativar();
        jogo.Ativo.Should().BeTrue();
    }
}
