using FCG.CatalogAPI.Application.Biblioteca.Commands;
using FCG.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FCG.CatalogAPI.Infrastructure.Mensageria;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly RegistrarItemBibliotecaHandler _handler;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(
        RegistrarItemBibliotecaHandler handler,
        ILogger<PaymentProcessedConsumer> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var evt = context.Message;

        _logger.LogInformation(
            "[CATALOG] 📥 PaymentProcessedEvent recebido | OrderId: {OrderId} | Status: {Status}",
            evt.OrderId, evt.Status);

        await _handler.HandleAsync(new RegistrarItemBibliotecaCommand(
            OrderId: evt.OrderId,
            UserId: evt.UserId,
            GameId: evt.GameId,
            GameName: evt.GameName,
            Status: evt.Status,
            TransactionId: evt.TransactionId,
            MotivoRejeicao: evt.MotivoRejeicao), context.CancellationToken);
    }
}
