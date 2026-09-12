using FCG.CatalogAPI.Application.Loja.CatalogoEstendido;
using FCG.CatalogAPI.Infrastructure.Persistencia.Mongo.Documents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FCG.CatalogAPI.Infrastructure.Persistencia.Mongo;

/// <summary>
/// Cria os índices MongoDB no startup, de forma idempotente. Deixa os índices
/// versionados em código (requisito da Fase 3).
/// </summary>
public class MongoIndexInitializer : BackgroundService
{
    private readonly MongoContext _ctx;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(MongoContext ctx, ILogger<MongoIndexInitializer> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // processed_events: índice único (consumer, messageId) → base da idempotência.
            var pe = _ctx.ProcessedEvents;
            var uniqueKey = Builders<ProcessedEventDocument>.IndexKeys
                .Ascending(x => x.Consumer)
                .Ascending(x => x.MessageId);
            await pe.Indexes.CreateOneAsync(
                new CreateIndexModel<ProcessedEventDocument>(
                    uniqueKey,
                    new CreateIndexOptions { Unique = true, Name = "ux_consumer_messageId" }),
                cancellationToken: stoppingToken);

            // TTL opcional em processedAt (0 = sem expiração, mantém histórico).
            if (_ctx.Options.ProcessedEventsTtlDays > 0)
            {
                await pe.Indexes.CreateOneAsync(
                    new CreateIndexModel<ProcessedEventDocument>(
                        Builders<ProcessedEventDocument>.IndexKeys.Ascending(x => x.ProcessedAt),
                        new CreateIndexOptions
                        {
                            Name = "ttl_processedAt",
                            ExpireAfter = TimeSpan.FromDays(_ctx.Options.ProcessedEventsTtlDays)
                        }),
                    cancellationToken: stoppingToken);
            }

            // game_catalog_extended: consultas por tag (multikey) e por gênero.
            var game = _ctx.GameCatalogExtended;
            await game.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<GameExtendedReadModel>(
                    Builders<GameExtendedReadModel>.IndexKeys.Ascending(x => x.Tags),
                    new CreateIndexOptions { Name = "ix_tags" }),
                new CreateIndexModel<GameExtendedReadModel>(
                    Builders<GameExtendedReadModel>.IndexKeys.Ascending(x => x.Genero),
                    new CreateIndexOptions { Name = "ix_genero" }),
            }, cancellationToken: stoppingToken);

            _logger.LogInformation("✅ Índices MongoDB criados/verificados (processed_events + game_catalog_extended).");
        }
        catch (Exception ex)
        {
            // Não derruba a aplicação por falha de índice; loga para diagnóstico.
            _logger.LogError(ex, "❌ Falha ao criar índices MongoDB. MongoDB está acessível?");
        }
    }
}
