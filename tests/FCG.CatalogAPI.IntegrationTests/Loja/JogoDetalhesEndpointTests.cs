using System.Net.Http.Headers;
using FCG.CatalogAPI.IntegrationTests.Infrastructure;

namespace FCG.CatalogAPI.IntegrationTests.Loja;

/// <summary>
/// Testes de integração do "catálogo estendido" (WS-B — read model rico em Mongo/fakes).
/// Usa <see cref="IsolatedCatalogWebApplicationFactory"/> (banco InMemory com nome único
/// por instância) em vez de <see cref="FcgCatalogWebApplicationFactory"/>, evitando colisão
/// com dados criados por outras classes de teste que compartilham o nome fixo
/// "fcg-catalog-tests" registrado em Program.cs.
/// </summary>
public class JogoDetalhesEndpointTests : IClassFixture<IsolatedCatalogWebApplicationFactory>
{
    private readonly IsolatedCatalogWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public JogoDetalhesEndpointTests(IsolatedCatalogWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private HttpClient CriarClienteComToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Guid> CriarJogoBaseAsync(HttpClient clienteAdmin)
    {
        var resp = await clienteAdmin.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-Detalhes-{Guid.NewGuid()}", descricao = "desc", genero = "RPG", preco = 49.90m });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task GetDetalhes_JogoBaseSemDocumento_DeveRetornar200ComLazySeed()
    {
        using var admin = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoBaseAsync(admin);

        var resp = await _client.GetAsync($"/api/jogos/{jogoId}/detalhes");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("gameId").GetGuid().Should().Be(jogoId);
        body.GetProperty("origem").GetString().Should().Be("lazy-seed");
    }

    [Fact]
    public async Task PostDetalhes_Admin_GravaEGetRetornaComOrigemAdmin()
    {
        using var admin = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoBaseAsync(admin);

        var postResp = await admin.PostAsJsonAsync($"/api/jogos/{jogoId}/detalhes", new
        {
            midia = new { coverUrl = "https://cdn/cover.png", bannerUrl = (string?)null, trailerUrl = (string?)null },
            screenshots = new[] { "https://cdn/s1.png" },
            tags = new[] { "rpg", "aventura" },
            requisitos = new
            {
                minimo = new { so = "Windows 10", cpu = "i5", gpu = "GTX 1050", ram = "8GB" },
                recomendado = (object?)null
            }
        });

        postResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResp = await _client.GetAsync($"/api/jogos/{jogoId}/detalhes");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("origem").GetString().Should().Be("admin");
        body.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).Should().Contain("rpg");
        body.GetProperty("midia").GetProperty("coverUrl").GetString().Should().Be("https://cdn/cover.png");
    }

    [Fact]
    public async Task GetDetalhes_JogoInexistente_DeveRetornar404()
    {
        var resp = await _client.GetAsync($"/api/jogos/{Guid.NewGuid()}/detalhes");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListarJogos_ComPipelineDeCache_DeveRetornar200()
    {
        // Smoke test: garante que o decorator de cache-aside (CachedListarJogosHandler,
        // com NoOpCatalogCache em ambiente Testing) não quebra o pipeline real da API.
        using var admin = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        await CriarJogoBaseAsync(admin);

        var resp = await _client.GetAsync("/api/jogos");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
