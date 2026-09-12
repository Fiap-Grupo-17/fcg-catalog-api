using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Infrastructure.Mensageria;
using FCG.Contracts.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FCG.CatalogAPI.Tests.Mensageria;

public class IdempotentConsumeFilterTests
{
    private static IdempotentConsumeFilter<PaymentProcessedEvent> CriarFiltro(IProcessedEventStore store)
        => new(store, NullLogger<IdempotentConsumeFilter<PaymentProcessedEvent>>.Instance);

    private static PaymentProcessedEvent CriarEvento(Guid orderId) => new(
        OrderId: orderId,
        UserId: Guid.NewGuid(),
        GameId: Guid.NewGuid(),
        GameName: "Jogo Teste",
        Price: 59.90m,
        Status: "Approved",
        TransactionId: "tx-1",
        MotivoRejeicao: null,
        ProcessedAt: DateTime.UtcNow);

    private static Mock<ConsumeContext<PaymentProcessedEvent>> CriarContexto(Guid? messageId, PaymentProcessedEvent mensagem)
    {
        var contexto = new Mock<ConsumeContext<PaymentProcessedEvent>>();
        contexto.SetupGet(c => c.MessageId).Returns(messageId);
        contexto.SetupGet(c => c.Message).Returns(mensagem);
        contexto.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return contexto;
    }

    private static Mock<IPipe<ConsumeContext<PaymentProcessedEvent>>> CriarProximoPipeDoPipeline()
    {
        var proximo = new Mock<IPipe<ConsumeContext<PaymentProcessedEvent>>>();
        proximo
            .Setup(p => p.Send(It.IsAny<ConsumeContext<PaymentProcessedEvent>>()))
            .Returns(Task.CompletedTask);
        return proximo;
    }

    [Fact]
    public async Task Send_PrimeiraMensagem_DeveChamarNext()
    {
        var store = new FakeProcessedEventStore();
        var filtro = CriarFiltro(store);
        var contexto = CriarContexto(Guid.NewGuid(), CriarEvento(Guid.NewGuid()));
        var proximo = CriarProximoPipeDoPipeline();

        await filtro.Send(contexto.Object, proximo.Object);

        proximo.Verify(p => p.Send(contexto.Object), Times.Once);
    }

    [Fact]
    public async Task Send_SegundaMensagemComMesmoMessageId_NaoDeveChamarNext()
    {
        var store = new FakeProcessedEventStore();
        var filtro = CriarFiltro(store);
        var messageId = Guid.NewGuid();

        var primeiroContexto = CriarContexto(messageId, CriarEvento(Guid.NewGuid()));
        var segundoContexto = CriarContexto(messageId, CriarEvento(Guid.NewGuid()));
        var proximo = CriarProximoPipeDoPipeline();

        await filtro.Send(primeiroContexto.Object, proximo.Object);
        await filtro.Send(segundoContexto.Object, proximo.Object);

        proximo.Verify(p => p.Send(It.IsAny<ConsumeContext<PaymentProcessedEvent>>()), Times.Once);
    }

    [Fact]
    public async Task Send_MessageIdNulo_UsaFallbackENuncaLancaExcecao()
    {
        var store = new FakeProcessedEventStore();
        var filtro = CriarFiltro(store);
        var contexto = CriarContexto(null, CriarEvento(Guid.NewGuid()));
        var proximo = CriarProximoPipeDoPipeline();

        Func<Task> act = () => filtro.Send(contexto.Object, proximo.Object);

        await act.Should().NotThrowAsync();
        proximo.Verify(p => p.Send(contexto.Object), Times.Once);
    }

    /// <summary>
    /// Fake in-memory de <see cref="IProcessedEventStore"/> usado só para provar, de forma
    /// isolada, a semântica de deduplicação por (consumer, messageId) exigida pelo filtro —
    /// espelha o comportamento do índice único do MongoProcessedEventStore real (WS-0).
    /// </summary>
    private sealed class FakeProcessedEventStore : IProcessedEventStore
    {
        private readonly HashSet<(string Consumer, Guid MessageId)> _registrados = new();

        public Task<bool> TryRegisterAsync(
            Guid messageId, string consumer, string? businessKey, string eventType, CancellationToken ct)
            => Task.FromResult(_registrados.Add((consumer, messageId)));
    }
}
