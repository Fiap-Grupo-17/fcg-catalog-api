using FCG.CatalogAPI.Application.Comum.Interfaces;
using MassTransit;

namespace FCG.CatalogAPI.Infrastructure.Mensageria;

public class MassTransitEventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventBus(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public Task PublicarAsync<T>(T evento, CancellationToken ct) where T : class
        => _publishEndpoint.Publish(evento, ct);
}
