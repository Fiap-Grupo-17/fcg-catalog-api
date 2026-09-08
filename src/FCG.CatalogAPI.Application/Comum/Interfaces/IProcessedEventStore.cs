namespace FCG.CatalogAPI.Application.Comum.Interfaces;

/// <summary>
/// Store de idempotência / log de eventos consumidos. Implementada na Infrastructure
/// sobre MongoDB (coleção <c>processed_events</c> com índice único
/// <c>(consumer, messageId)</c>). Base da deduplicação de consumers.
/// </summary>
public interface IProcessedEventStore
{
    /// <summary>
    /// Tenta registrar o processamento de uma mensagem de forma atômica.
    /// </summary>
    /// <returns>
    /// <c>true</c> se registrou agora (primeira vez — deve processar);
    /// <c>false</c> se já existia (duplicado — deve ignorar).
    /// </returns>
    Task<bool> TryRegisterAsync(
        Guid messageId,
        string consumer,
        string? businessKey,
        string eventType,
        CancellationToken ct);
}
