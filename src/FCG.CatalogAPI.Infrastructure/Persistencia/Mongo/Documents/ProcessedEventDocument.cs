using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FCG.CatalogAPI.Infrastructure.Persistencia.Mongo.Documents;

/// <summary>
/// Documento da coleção <c>processed_events</c>: registro atômico de mensagens já
/// consumidas (idempotência) e log de eventos. Índice único <c>(consumer, messageId)</c>.
/// </summary>
public class ProcessedEventDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("messageId")]
    [BsonRepresentation(BsonType.String)]
    public Guid MessageId { get; set; }

    /// <summary>Nome do consumer/tipo do evento (ex.: "PaymentProcessedEvent").</summary>
    [BsonElement("consumer")]
    public string Consumer { get; set; } = string.Empty;

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("businessKey")]
    [BsonIgnoreIfNull]
    public string? BusinessKey { get; set; }

    [BsonElement("processedAt")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
