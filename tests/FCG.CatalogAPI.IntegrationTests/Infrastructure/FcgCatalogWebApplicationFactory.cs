using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FCG.CatalogAPI.IntegrationTests.Infrastructure;

public class FcgCatalogWebApplicationFactory : WebApplicationFactory<Program>
{
    static FcgCatalogWebApplicationFactory()
    {
        // Garante que WebApplication.CreateBuilder() leia appsettings.Testing.json,
        // que ativa UseInMemoryDatabase=true.
        // Deve ser definido ANTES de qualquer instância do factory ser criada.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Substitui IEventBus real (MassTransit) por NoOp
            var ebDescriptors = services
                .Where(d => d.ServiceType == typeof(IEventBus))
                .ToList();
            foreach (var d in ebDescriptors)
                services.Remove(d);
            services.AddScoped<IEventBus, NoOpEventBus>();

            // Remove todos os IHostedService (MassTransit / RabbitMQ)
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var s in hostedServices)
                services.Remove(s);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var host = base.CreateHost(builder);

        // Garante que o schema InMemory está criado após o host estar pronto
        using var scope = host.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        ctx.Database.EnsureCreated();

        return host;
    }
}

/// <summary>Implementação NoOp de IEventBus para testes de integração.</summary>
public class NoOpEventBus : IEventBus
{
    public Task PublicarAsync<T>(T evento, CancellationToken ct = default) where T : class
        => Task.CompletedTask;
}
