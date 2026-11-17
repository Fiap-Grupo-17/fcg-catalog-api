using Elastic.Clients.Elasticsearch;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FCG.CatalogAPI.Infrastructure.Busca;

/// <summary>
/// No startup: cria o índice (se não existir) e faz a SINCRONIZAÇÃO COMPLETA com o Postgres
/// (indexa os jogos ativos e remove os inativos). Assim os jogos cadastrados antes da busca
/// existir — ou durante uma queda do Elasticsearch — entram no índice.
/// Não derruba a API se o Elasticsearch estiver fora: tenta de novo a cada 30 s.
/// </summary>
public class ElasticsearchIndexInitializer : BackgroundService
{
    private static readonly TimeSpan IntervaloNovaTentativa = TimeSpan.FromSeconds(30);

    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ElasticsearchIndexInitializer> _logger;

    public ElasticsearchIndexInitializer(
        ElasticsearchClient client,
        ElasticsearchOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<ElasticsearchIndexInitializer> logger)
    {
        _client = client;
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GarantirIndiceAsync(stoppingToken);
                await SincronizarAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "⚠️ [BUSCA] Elasticsearch ainda indisponível em {Url}; nova tentativa em {Segundos}s.",
                    _options.Url, IntervaloNovaTentativa.TotalSeconds);
            }

            try { await Task.Delay(IntervaloNovaTentativa, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task GarantirIndiceAsync(CancellationToken ct)
    {
        var existe = await _client.Indices.ExistsAsync(_options.IndexName, ct);
        if (existe.Exists) return;

        // Nó único: 0 réplicas → índice "green". Mapeamento dinâmico: textos viram text + keyword.
        var criado = await _client.Indices.CreateAsync(_options.IndexName, c => c
            .Settings(s => s.NumberOfShards(1).NumberOfReplicas(0)), ct);

        if (!criado.IsValidResponse && criado.ElasticsearchServerError?.Error?.Type != "resource_already_exists_exception")
            throw new InvalidOperationException($"Falha ao criar o índice '{_options.IndexName}': {criado.DebugInformation}");

        _logger.LogInformation("✅ [BUSCA] Índice '{Indice}' criado.", _options.IndexName);
    }

    private async Task SincronizarAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICatalogDbContext>();
        var jogos = await db.Jogos.AsNoTracking().ToListAsync(ct);

        var ativos = jogos.Where(j => j.Ativo).Select(JogoDocumento.De).ToList();
        var inativos = jogos.Where(j => !j.Ativo).Select(j => j.Id.ToString()).ToList();

        if (ativos.Count > 0)
        {
            var bulk = await _client.BulkAsync(b => b
                .Index(_options.IndexName)
                .IndexMany(ativos, (op, doc) => op.Id(doc.Id.ToString())), ct);
            if (!bulk.IsValidResponse || bulk.Errors)
                throw new InvalidOperationException($"Falha na carga do índice: {bulk.DebugInformation}");
        }

        foreach (var id in inativos)
            await _client.DeleteAsync(new DeleteRequest(_options.IndexName, id), ct); // 404 é ok

        await _client.Indices.RefreshAsync(_options.IndexName, ct);

        _logger.LogInformation(
            "✅ [BUSCA] Índice '{Indice}' sincronizado com o Postgres | ativos indexados: {Ativos} | inativos removidos: {Inativos}",
            _options.IndexName, ativos.Count, inativos.Count);
    }
}
