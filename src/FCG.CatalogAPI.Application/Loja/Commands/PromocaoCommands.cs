using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────

public record CriarPromocaoCommand(
    string Nome,
    decimal PercentualDesconto,
    DateTime Inicio,
    DateTime Fim,
    IReadOnlyList<Guid> JogosIds);

public record AtualizarPromocaoCommand(
    string Nome,
    decimal PercentualDesconto,
    DateTime Inicio,
    DateTime Fim,
    IReadOnlyList<Guid> JogosIds);

public record PromocaoResult(
    Guid Id,
    string Nome,
    decimal PercentualDesconto,
    DateTime Inicio,
    DateTime Fim,
    bool Ativa,
    IReadOnlyList<Guid> JogosIds);

// ── CriarPromocaoHandler ──────────────────────────────────────────────────

public class CriarPromocaoHandler
{
    private readonly ICatalogDbContext _db;
    public CriarPromocaoHandler(ICatalogDbContext db) => _db = db;

    public async Task<PromocaoResult> HandleAsync(CriarPromocaoCommand cmd, CancellationToken ct)
    {
        // Valida que todos os jogos informados existem e estão ativos
        var jogosIds = cmd.JogosIds.Distinct().ToList();
        var jogosEncontrados = await _db.Jogos
            .Where(j => jogosIds.Contains(j.Id) && j.Ativo)
            .Select(j => j.Id)
            .ToListAsync(ct);

        if (jogosEncontrados.Count != jogosIds.Count)
            throw new ArgumentException(
                "Um ou mais jogos informados não existem ou estão inativos.");

        var promocao = Promocao.Criar(cmd.Nome, cmd.PercentualDesconto,
            cmd.Inicio, cmd.Fim, jogosIds);

        _db.Promocoes.Add(promocao);
        await _db.SaveChangesAsync(ct);

        return PromocaoMapper.ToResult(promocao);
    }
}

// ── AtualizarPromocaoHandler ──────────────────────────────────────────────

public class AtualizarPromocaoHandler
{
    private readonly ICatalogDbContext _db;
    public AtualizarPromocaoHandler(ICatalogDbContext db) => _db = db;

    public async Task<PromocaoResult?> HandleAsync(
        Guid id, AtualizarPromocaoCommand cmd, CancellationToken ct)
    {
        // ── Validação antecipada: garante 400 antes de tocar o banco ──────────
        // Sem isso, ID inexistente retornaria 404 mesmo com dados inválidos.
        if (string.IsNullOrWhiteSpace(cmd.Nome))
            throw new ArgumentException("Nome da promoção é obrigatório.");
        if (cmd.PercentualDesconto <= 0 || cmd.PercentualDesconto > 100)
            throw new ArgumentException("Percentual de desconto deve ser entre 1 e 100.");
        if (cmd.Fim <= cmd.Inicio)
            throw new ArgumentException("A data de fim deve ser posterior à data de início.");
        var jogosIds = cmd.JogosIds?.Distinct().ToList() ?? new List<Guid>();
        if (jogosIds.Count == 0)
            throw new ArgumentException("A promoção deve conter ao menos um jogo.");

        // ── Busca no banco somente após validação básica ───────────────────────
        var promocao = await _db.Promocoes.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (promocao is null) return null;

        var jogosEncontrados = await _db.Jogos
            .Where(j => jogosIds.Contains(j.Id) && j.Ativo)
            .Select(j => j.Id)
            .ToListAsync(ct);

        if (jogosEncontrados.Count != jogosIds.Count)
            throw new ArgumentException(
                "Um ou mais jogos informados não existem ou estão inativos.");

        promocao.Atualizar(cmd.Nome, cmd.PercentualDesconto,
            cmd.Inicio, cmd.Fim, jogosIds);

        await _db.SaveChangesAsync(ct);
        return PromocaoMapper.ToResult(promocao);
    }
}

// ── EncerrarPromocaoHandler ───────────────────────────────────────────────

public class EncerrarPromocaoHandler
{
    private readonly ICatalogDbContext _db;
    public EncerrarPromocaoHandler(ICatalogDbContext db) => _db = db;

    public async Task<bool> HandleAsync(Guid id, CancellationToken ct)
    {
        var promocao = await _db.Promocoes.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (promocao is null) return false;

        promocao.Encerrar();
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Helper ────────────────────────────────────────────────────────────────

file static class PromocaoMapper
{
    internal static PromocaoResult ToResult(Promocao p) =>
        new(p.Id, p.Nome, p.PercentualDesconto, p.Inicio, p.Fim, p.Ativa, p.JogosIds);
}
