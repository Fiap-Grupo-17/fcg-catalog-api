using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Biblioteca.Entidades;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace FCG.CatalogAPI.Infrastructure.Persistencia;

public class CatalogDbContext : DbContext, ICatalogDbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Jogo> Jogos => Set<Jogo>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemBiblioteca> ItensBiblioteca => Set<ItemBiblioteca>();
    public DbSet<Promocao> Promocoes => Set<Promocao>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("loja");

        // Jogo
        mb.Entity<Jogo>(e =>
        {
            e.ToTable("jogos");
            e.HasKey(j => j.Id);
            e.Property(j => j.Titulo).HasMaxLength(200).IsRequired();
            e.Property(j => j.Descricao).HasMaxLength(2000);
            e.Property(j => j.Genero).HasMaxLength(100);
            e.Property(j => j.Preco).HasPrecision(10, 2);
        });

        // Pedido
        mb.Entity<Pedido>(e =>
        {
            e.ToTable("pedidos");
            e.HasKey(p => p.Id);
            e.Property(p => p.JogoTitulo).HasMaxLength(200);
            e.Property(p => p.Valor).HasPrecision(10, 2);
            e.Property(p => p.Status).HasConversion<string>();
            e.Property(p => p.TransacaoId).HasMaxLength(100);
            e.Property(p => p.MotivoRejeicao).HasMaxLength(500);
        });

        // ItemBiblioteca — schema separado
        mb.Entity<ItemBiblioteca>(e =>
        {
            e.ToTable("itens", "biblioteca");
            e.HasKey(i => i.Id);
            e.Property(i => i.JogoTitulo).HasMaxLength(200);
            e.HasIndex(i => new { i.UsuarioId, i.JogoId }).IsUnique();
        });

        // Promocao — armazena lista de GUIDs como JSON no PostgreSQL
        var guidListConverter = new ValueConverter<List<Guid>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>());

        var guidListComparer = new ValueComparer<List<Guid>>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Aggregate(0, (a, g) => HashCode.Combine(a, g.GetHashCode())),
            v => v.ToList());

        mb.Entity<Promocao>(e =>
        {
            e.ToTable("promocoes");
            e.HasKey(p => p.Id);
            e.Property(p => p.Nome).HasMaxLength(200).IsRequired();
            e.Property(p => p.PercentualDesconto).HasPrecision(5, 2);

            // Mapeia o backing field _jogosIds como coluna JSON
            e.Property<List<Guid>>("_jogosIds")
             .HasColumnName("jogos_ids")
             .HasColumnType("jsonb")
             .HasConversion(guidListConverter, guidListComparer)
             .IsRequired();
        });
    }
}
