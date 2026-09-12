using FCG.CatalogAPI.Application.Comum.Interfaces;

namespace FCG.CatalogAPI.Application.Comum.Cache;

/// <summary>
/// Implementação nula (no-op) de <see cref="ICatalogCache"/>, usada apenas como valor
/// padrão nos construtores de conveniência dos command handlers de Loja (CriarJogoHandler,
/// AtualizarJogoHandler, DesativarJogoHandler, AtivarJogoHandler). Existe para preservar
/// compatibilidade binária com código/testes que instanciam esses handlers passando
/// somente o ICatalogDbContext, sem precisar tocar nesses arquivos de teste (fora do
/// escopo deste workstream). Em runtime real, o container de DI sempre resolve o
/// construtor com ICatalogCache explícito (Redis), por ser o construtor público com o
/// maior número de parâmetros satisfazíveis — comportamento padrão do
/// Microsoft.Extensions.DependencyInjection.
/// </summary>
internal sealed class CacheNulo : ICatalogCache
{
    public static readonly CacheNulo Instancia = new();

    private CacheNulo() { }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct) => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct) => Task.CompletedTask;
}
