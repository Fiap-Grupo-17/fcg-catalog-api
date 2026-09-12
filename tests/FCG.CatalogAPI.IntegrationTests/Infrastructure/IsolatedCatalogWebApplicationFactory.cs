using FCG.CatalogAPI.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.CatalogAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Variante de <see cref="FcgCatalogWebApplicationFactory"/> com banco InMemory ISOLADO
/// por instância (nome único via <see cref="Guid.NewGuid"/>), em vez do nome fixo
/// <c>"fcg-catalog-tests"</c> hardcoded em <c>Program.cs</c>.
///
/// Motivo: o nome fixo é compartilhado por TODAS as classes de teste que usam
/// <see cref="FcgCatalogWebApplicationFactory"/> (via <c>IClassFixture</c>), o que causa
/// colisão de dados quando múltiplas classes/execuções (inclusive de outros workstreams)
/// rodam em paralelo contra o mesmo provider EF InMemory (que mantém o "banco" em memória
/// do processo, chaveado pelo nome). Esta factory reconfigura o <see cref="DbContextOptions{TContext}"/>
/// de <see cref="CatalogDbContext"/> DEPOIS do <c>Program.cs</c> registrar a versão com nome
/// fixo, sem exigir nenhuma alteração no código de produção ou na factory dos colegas.
/// </summary>
public class IsolatedCatalogWebApplicationFactory : FcgCatalogWebApplicationFactory
{
    private readonly string _nomeBancoIsolado = $"fcg-catalog-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var descritoresParaRemover = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>)
                         || d.ServiceType == typeof(CatalogDbContext))
                .ToList();
            foreach (var descritor in descritoresParaRemover)
                services.Remove(descritor);

            services.AddDbContext<CatalogDbContext>(opt => opt.UseInMemoryDatabase(_nomeBancoIsolado));
        });
    }
}
