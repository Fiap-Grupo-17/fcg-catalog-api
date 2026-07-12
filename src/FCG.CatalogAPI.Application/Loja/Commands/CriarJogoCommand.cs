using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public record CriarJogoCommand(string Titulo, string Descricao, string Genero, decimal Preco);

public record CriarJogoResult(Guid Id, string Titulo, decimal Preco);

public class CriarJogoHandler
{
    private readonly ICatalogDbContext _db;

    public CriarJogoHandler(ICatalogDbContext db) => _db = db;

    public async Task<CriarJogoResult> HandleAsync(CriarJogoCommand cmd, CancellationToken ct)
    {
        var jogo = Jogo.Criar(cmd.Titulo, cmd.Descricao, cmd.Genero, cmd.Preco);
        _db.Jogos.Add(jogo);
        await _db.SaveChangesAsync(ct);
        return new CriarJogoResult(jogo.Id, jogo.Titulo, jogo.Preco);
    }
}
