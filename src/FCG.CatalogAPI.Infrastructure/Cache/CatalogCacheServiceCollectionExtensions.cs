using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.CatalogAPI.Infrastructure.Cache;

/// <summary>
/// Registra os decorators de cache-aside dos casos de uso de leitura do catálogo
/// (WS-A). Pressupõe que <c>ListarJogosHandler</c>, <c>BuscarJogoHandler</c> e
/// <see cref="ICatalogCache"/> concretos já estão registrados no container (Program.cs);
/// este método NÃO os re-registra, apenas expõe as interfaces
/// <see cref="IListarJogosHandler"/>/<see cref="IBuscarJogoHandler"/> decoradas com cache,
/// que passam a ser o que os endpoints Minimal API devem injetar.
/// </summary>
public static class CatalogCacheServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogReadCaching(this IServiceCollection services)
    {
        services.AddScoped<IListarJogosHandler>(sp =>
            new CachedListarJogosHandler(
                sp.GetRequiredService<ListarJogosHandler>(),
                sp.GetRequiredService<ICatalogCache>()));

        services.AddScoped<IBuscarJogoHandler>(sp =>
            new CachedBuscarJogoHandler(
                sp.GetRequiredService<BuscarJogoHandler>(),
                sp.GetRequiredService<ICatalogCache>()));

        return services;
    }
}
