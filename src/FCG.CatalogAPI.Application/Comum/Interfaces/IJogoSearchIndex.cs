using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Application.Comum.Interfaces;

/// <summary>
/// Índice de busca avançada do catálogo (TC4, item 3). Implementado na Infrastructure
/// sobre Elasticsearch. O Postgres continua sendo a fonte da verdade: o índice é uma
/// cópia otimizada para busca textual (fuzzy + relevância), atualizada pelos command handlers.
/// </summary>
public interface IJogoSearchIndex
{
    /// <summary>Insere ou sobrescreve o documento do jogo (Id do jogo = Id do documento).</summary>
    Task IndexarAsync(Jogo jogo, CancellationToken ct);

    /// <summary>Remove o jogo do índice (ex.: jogo desativado não aparece na busca).</summary>
    Task RemoverAsync(Guid jogoId, CancellationToken ct);

    /// <summary>Busca fuzzy, ordenada por relevância (maior score primeiro).</summary>
    Task<IReadOnlyList<JogoBuscaDto>> BuscarAsync(string termo, int tamanho, CancellationToken ct);
}

/// <summary>Resultado da busca: dados do jogo + score de relevância calculado pelo motor de busca.</summary>
public record JogoBuscaDto(Guid Id, string Titulo, string Descricao, string Genero, decimal Preco, double? Score);

/// <summary>O motor de busca está fora do ar ou não respondeu.</summary>
public class BuscaIndisponivelException : Exception
{
    public BuscaIndisponivelException(string message, Exception? inner = null) : base(message, inner) { }
}
