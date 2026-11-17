using FCG.CatalogAPI.Application.Comum.Interfaces;

namespace FCG.CatalogAPI.Application.Loja.Busca;

/// <summary>
/// Busca avançada de jogos (GET /api/jogos/search). Consulta SOMENTE o índice de busca
/// (Elasticsearch), nunca o banco relacional — requisito do item 3 do TC4.
/// </summary>
public class BuscarJogosPorTextoHandler
{
    public const int TamanhoPadrao = 20;
    public const int TamanhoMaximo = 50;

    private readonly IJogoSearchIndex _indice;

    public BuscarJogosPorTextoHandler(IJogoSearchIndex indice) => _indice = indice;

    public Task<IReadOnlyList<JogoBuscaDto>> HandleAsync(string? termo, int? tamanho, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(termo))
            throw new ArgumentException("Informe o termo de busca no parâmetro 'q'.");

        var limite = Math.Clamp(tamanho ?? TamanhoPadrao, 1, TamanhoMaximo);
        return _indice.BuscarAsync(termo.Trim(), limite, ct);
    }
}
