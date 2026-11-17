using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Application.Comum.Busca;

/// <summary>
/// Implementação nula (no-op) de <see cref="IJogoSearchIndex"/>, usada só nos construtores de
/// conveniência dos command handlers (mesmo papel do <see cref="Cache.CacheNulo"/>). Em runtime,
/// o container de DI resolve sempre o construtor completo com o índice real.
/// </summary>
internal sealed class IndiceBuscaNulo : IJogoSearchIndex
{
    public static readonly IndiceBuscaNulo Instancia = new();

    private IndiceBuscaNulo() { }

    public Task IndexarAsync(Jogo jogo, CancellationToken ct) => Task.CompletedTask;

    public Task RemoverAsync(Guid jogoId, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<JogoBuscaDto>> BuscarAsync(string termo, int tamanho, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<JogoBuscaDto>>(Array.Empty<JogoBuscaDto>());
}
