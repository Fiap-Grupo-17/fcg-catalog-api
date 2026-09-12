using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Infrastructure.Persistencia.Mongo.Documents;
using MongoDB.Driver;

namespace FCG.CatalogAPI.Infrastructure.Persistencia.Mongo;

/// <summary>
/// Idempotência baseada em MongoDB. A atomicidade vem do índice único
/// <c>(consumer, messageId)</c>: uma inserção duplicada lança E11000, capturada
/// e tratada como "já processado".
/// </summary>
public class MongoProcessedEventStore : IProcessedEventStore
{
    private readonly MongoContext _ctx;

    public MongoProcessedEventStore(MongoContext ctx) => _ctx = ctx;

    public async Task<bool> TryRegisterAsync(
        Guid messageId,
        string consumer,
        string? businessKey,
        string eventType,
        CancellationToken ct)
    {
        var doc = new ProcessedEventDocument
        {
            MessageId = messageId,
            Consumer = consumer,
            EventType = eventType,
            BusinessKey = businessKey,
            ProcessedAt = DateTime.UtcNow
        };

        try
        {
            await _ctx.ProcessedEvents.InsertOneAsync(doc, options: null, ct);
            return true; // primeira vez
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false; // já processado (duplicado)
        }
    }
}
