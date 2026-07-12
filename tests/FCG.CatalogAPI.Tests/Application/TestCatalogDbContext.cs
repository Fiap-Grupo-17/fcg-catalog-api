using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Biblioteca.Entidades;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Tests.Application;

/// <summary>
/// DbContext concreto usando InMemory para testes de handlers que dependem de ICatalogDbContext.
/// </summary>
public class TestCatalogDbContext : DbContext, ICatalogDbContext
{
    public TestCatalogDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Jogo> Jogos => Set<Jogo>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemBiblioteca> ItensBiblioteca => Set<ItemBiblioteca>();
    public DbSet<Promocao> Promocoes => Set<Promocao>();

    /// <summary>Cria uma instância com banco InMemory isolado por nome único.</summary>
    public static TestCatalogDbContext Criar(string? nome = null)
    {
        var opts = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nome ?? Guid.NewGuid().ToString())
            .Options;
        return new TestCatalogDbContext(opts);
    }
}
