using FCG.CatalogAPI.Domain.Loja.Entidades;
using FCG.CatalogAPI.Domain.Biblioteca.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Comum.Interfaces;

public interface ICatalogDbContext
{
    DbSet<Jogo> Jogos { get; }
    DbSet<Pedido> Pedidos { get; }
    DbSet<ItemBiblioteca> ItensBiblioteca { get; }
    DbSet<Promocao> Promocoes { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
