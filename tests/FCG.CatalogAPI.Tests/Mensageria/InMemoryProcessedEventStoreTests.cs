using FCG.CatalogAPI.Infrastructure.Testing;
using FluentAssertions;

namespace FCG.CatalogAPI.Tests.Mensageria;

/// <summary>
/// Testa diretamente <see cref="InMemoryProcessedEventStore"/> (o fake usado em Program.cs
/// no ambiente Testing), garantindo que a semântica de idempotência por (consumer, messageId)
/// — já exercitada indiretamente em <see cref="IdempotentConsumeFilterTests"/> via um fake
/// local — também se sustenta na implementação concreta que roda de fato na suíte de
/// integração e no pipeline MassTransit em ambiente Testing.
/// </summary>
public class InMemoryProcessedEventStoreTests
{
    [Fact]
    public async Task TryRegisterAsync_PrimeiraOcorrencia_DeveRetornarTrue()
    {
        var store = new InMemoryProcessedEventStore();

        var registrado = await store.TryRegisterAsync(
            Guid.NewGuid(), "consumer-a", "chave-negocio", "EventoTeste", CancellationToken.None);

        registrado.Should().BeTrue();
    }

    [Fact]
    public async Task TryRegisterAsync_MesmoConsumerEMessageId_SegundaChamadaDeveRetornarFalse()
    {
        var store = new InMemoryProcessedEventStore();
        var messageId = Guid.NewGuid();

        var primeira = await store.TryRegisterAsync(messageId, "consumer-a", null, "EventoTeste", CancellationToken.None);
        var segunda = await store.TryRegisterAsync(messageId, "consumer-a", null, "EventoTeste", CancellationToken.None);

        primeira.Should().BeTrue();
        segunda.Should().BeFalse();
    }

    [Fact]
    public async Task TryRegisterAsync_MesmoMessageIdEmConsumersDiferentes_AmbosDevemRetornarTrue()
    {
        var store = new InMemoryProcessedEventStore();
        var messageId = Guid.NewGuid();

        var consumerA = await store.TryRegisterAsync(messageId, "consumer-a", null, "EventoTeste", CancellationToken.None);
        var consumerB = await store.TryRegisterAsync(messageId, "consumer-b", null, "EventoTeste", CancellationToken.None);

        consumerA.Should().BeTrue();
        consumerB.Should().BeTrue();
    }
}
