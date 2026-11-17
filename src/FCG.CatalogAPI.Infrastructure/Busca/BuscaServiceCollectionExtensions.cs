using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.CatalogAPI.Infrastructure.Busca;

public static class BuscaServiceCollectionExtensions
{
    /// <summary>
    /// Registra o cliente oficial do Elasticsearch (singleton, thread-safe), o índice de busca
    /// e o inicializador (criação do índice + sincronização com o Postgres no startup).
    /// </summary>
    public static IServiceCollection AddBuscaElasticsearch(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new ElasticsearchOptions();
        configuration.GetSection(ElasticsearchOptions.SectionName).Bind(options);
        services.AddSingleton(options);

        var settings = new ElasticsearchClientSettings(new Uri(options.Url))
            .DefaultIndex(options.IndexName)
            .RequestTimeout(TimeSpan.FromSeconds(10));

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
            settings = settings.Authentication(new ApiKey(options.ApiKey));
        else if (!string.IsNullOrWhiteSpace(options.Username))
            settings = settings.Authentication(new BasicAuthentication(options.Username, options.Password ?? string.Empty));

        services.AddSingleton(new ElasticsearchClient(settings));
        services.AddSingleton<IJogoSearchIndex, ElasticsearchJogoSearchIndex>();
        services.AddHostedService<ElasticsearchIndexInitializer>();
        return services;
    }
}
