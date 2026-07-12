using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public record AtualizarJogoCommand(string Titulo, string Descricao, string Genero, decimal Preco);
public record AtualizarJogoResult(Guid Id, string Titulo, decimal Preco);

public class AtualizarJogoHandler
{
    private readonly ICatalogDbContext _db;
    public AtualizarJogoHandler(ICatalogDbContext db) => _db = db;

    public async Task<AtualizarJogoResult?> HandleAsync(Guid id, AtualizarJogoCommand cmd, CancellationToken ct)
    {
        var jogo = await _db.Jogos.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (jogo is null) return null;

        jogo.Atualizar(cmd.Titulo, cmd.Descricao, cmd.Genero, cmd.Preco);
        await _db.SaveChangesAsync(ct);

        return new AtualizarJogoResult(jogo.Id, jogo.Titulo, jogo.Preco);
    }
}
