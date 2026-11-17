using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using Microsoft.Extensions.Logging;

namespace FCG.CatalogAPI.Infrastructure.Busca;

/// <summary>
/// Índice de busca sobre Elasticsearch (TC4, item 3).
/// - Indexar/Remover: chamados pelos command handlers logo após o SaveChanges no Postgres.
///   Uma falha aqui NÃO desfaz a gravação (o Postgres é a fonte da verdade): só loga o erro;
///   a sincronização completa do startup (<see cref="ElasticsearchIndexInitializer"/>) corrige.
/// - Buscar: multi_match com fuzziness AUTO (tolera erros de digitação), ordenado por _score.
/// </summary>
public class ElasticsearchJogoSearchIndex : IJogoSearchIndex
{
    // Pesos: um acerto no título vale mais que no gênero, que vale mais que na descrição.
    private static readonly string[] CamposBusca = { "titulo^3", "genero^2", "descricao" };

    private readonly ElasticsearchClient _client;
    private readonly string _indice;
    private readonly ILogger<ElasticsearchJogoSearchIndex> _logger;

    public ElasticsearchJogoSearchIndex(
        ElasticsearchClient client,
        ElasticsearchOptions options,
        ILogger<ElasticsearchJogoSearchIndex> logger)
    {
        _client = client;
        _indice = options.IndexName;
        _logger = logger;
    }

    public async Task IndexarAsync(Jogo jogo, CancellationToken ct)
    {
        try
        {
            var doc = JogoDocumento.De(jogo);
            // Refresh=WaitFor: o documento já aparece na próxima busca (útil na demo "cadastra e busca").
            var resp = await _client.IndexAsync(doc, i => i
                .Index(_indice)
                .Id(doc.Id.ToString())
                .Refresh(Refresh.WaitFor), ct);

            if (resp.IsValidResponse)
                _logger.LogInformation("🔎 [BUSCA] Jogo indexado | Id: {Id} | Título: {Titulo}", jogo.Id, jogo.Titulo);
            else
                _logger.LogError("❌ [BUSCA] Falha ao indexar jogo {Id}: {Erro}", jogo.Id, resp.DebugInformation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [BUSCA] Elasticsearch indisponível ao indexar o jogo {Id}", jogo.Id);
        }
    }

    public async Task RemoverAsync(Guid jogoId, CancellationToken ct)
    {
        try
        {
            var resp = await _client.DeleteAsync(new DeleteRequest(_indice, jogoId.ToString())
            {
                Refresh = Refresh.WaitFor
            }, ct);

            // 404 = já não estava no índice: tudo certo.
            if (resp.IsValidResponse || resp.Result == Result.NotFound)
                _logger.LogInformation("🔎 [BUSCA] Jogo removido do índice | Id: {Id}", jogoId);
            else
                _logger.LogError("❌ [BUSCA] Falha ao remover jogo {Id} do índice: {Erro}", jogoId, resp.DebugInformation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [BUSCA] Elasticsearch indisponível ao remover o jogo {Id}", jogoId);
        }
    }

    public async Task<IReadOnlyList<JogoBuscaDto>> BuscarAsync(string termo, int tamanho, CancellationToken ct)
    {
        SearchResponse<JogoDocumento> resp;
        try
        {
            resp = await _client.SearchAsync<JogoDocumento>(s => s
                .Index(_indice)
                .Size(tamanho)
                .Query(q => q.MultiMatch(m => m
                    .Query(termo)
                    .Fields(CamposBusca)
                    .Fuzziness(new Fuzziness("AUTO")))), ct);
            // Sem .Sort(...): o Elasticsearch ordena por _score (relevância), do maior para o menor.
        }
        catch (Exception ex)
        {
            throw new BuscaIndisponivelException("Serviço de busca indisponível no momento.", ex);
        }

        if (!resp.IsValidResponse)
        {
            // Índice ainda não criado (nenhum jogo indexado): busca vazia, não erro.
            if (resp.ElasticsearchServerError?.Error?.Type == "index_not_found_exception")
                return Array.Empty<JogoBuscaDto>();

            _logger.LogError("❌ [BUSCA] Falha na busca por '{Termo}': {Erro}", termo, resp.DebugInformation);
            throw new BuscaIndisponivelException("Serviço de busca indisponível no momento.");
        }

        return resp.Hits
            .Where(h => h.Source is not null)
            .Select(h => new JogoBuscaDto(
                h.Source!.Id, h.Source.Titulo, h.Source.Descricao, h.Source.Genero, h.Source.Preco, h.Score))
            .ToList();
    }
}
