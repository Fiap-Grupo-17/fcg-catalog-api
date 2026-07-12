namespace FCG.CatalogAPI.Application.Comum.Interfaces;

public interface IEventBus
{
    Task PublicarAsync<T>(T evento, CancellationToken ct) where T : class;
}
