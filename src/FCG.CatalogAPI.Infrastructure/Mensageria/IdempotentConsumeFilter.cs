using System.Reflection;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FCG.CatalogAPI.Infrastructure.Mensageria;

/// <summary>
/// Filtro genérico de consumo do MassTransit que garante idempotência de qualquer
/// consumer registrado no barramento, deduplicando pelo <see cref="ConsumeContext.MessageId"/>
/// do envelope (não pelo payload — os eventos do FCG não trazem um EventId próprio).
/// A persistência do registro fica a cargo de <see cref="IProcessedEventStore"/>
/// (MongoDB em produção, com índice único (consumer, messageId); in-memory em Testing).
/// </summary>
public class IdempotentConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly IProcessedEventStore _store;
    private readonly ILogger<IdempotentConsumeFilter<T>> _logger;

    public IdempotentConsumeFilter(IProcessedEventStore store, ILogger<IdempotentConsumeFilter<T>> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        // MessageId é preenchido automaticamente pelo MassTransit em todo Publish/Send;
        // o fallback com NewId é apenas defensivo (broker/origem sem MessageId).
        var messageId = context.MessageId ?? NewId.NextGuid();
        var consumer = typeof(T).Name;
        var eventType = typeof(T).FullName ?? consumer;
        var businessKey = ExtrairChaveDeNegocio(context.Message);

        var primeiraVez = await _store.TryRegisterAsync(
            messageId, consumer, businessKey, eventType, context.CancellationToken);

        if (!primeiraVez)
        {
            _logger.LogInformation(
                "Evento duplicado ignorado | Consumer: {Consumer} | MessageId: {MessageId} | BusinessKey: {BusinessKey}",
                consumer, messageId, businessKey);
            return;
        }

        await next.Send(context);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("idempotent");

    /// <summary>
    /// Extração best-effort (via reflection) de "OrderId" ou "UserId" da mensagem, apenas
    /// como metadado de rastreabilidade no store — a chave real de deduplicação é sempre o
    /// MessageId do envelope. Nunca lança: mensagens sem essas propriedades ficam com null.
    /// </summary>
    private static string? ExtrairChaveDeNegocio(T mensagem)
    {
        try
        {
            var tipo = typeof(T);
            var propriedade = tipo.GetProperty("OrderId", BindingFlags.Public | BindingFlags.Instance)
                ?? tipo.GetProperty("UserId", BindingFlags.Public | BindingFlags.Instance);

            if (propriedade is null || propriedade.PropertyType != typeof(Guid))
                return null;

            return propriedade.GetValue(mensagem) is Guid valor ? valor.ToString() : null;
        }
        catch
        {
            return null;
        }
    }
}
