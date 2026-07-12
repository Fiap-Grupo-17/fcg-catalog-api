using System.Net.Http.Headers;
using FCG.CatalogAPI.IntegrationTests.Infrastructure;

namespace FCG.CatalogAPI.IntegrationTests.Biblioteca;

public class BibliotecaEndpointTests : IClassFixture<FcgCatalogWebApplicationFactory>
{
    private readonly FcgCatalogWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BibliotecaEndpointTests(FcgCatalogWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListarBiblioteca_UsuarioAutenticado_DeveRetornar200()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenUsuario());
        var resp = await client.GetAsync("/api/biblioteca");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListarBiblioteca_SemAutenticacao_DeveRetornar401()
    {
        var resp = await _client.GetAsync("/api/biblioteca");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IniciarAquisicao_SemAutenticacao_DeveRetornar401()
    {
        var resp = await _client.PostAsJsonAsync("/api/biblioteca/aquisicoes",
            new { jogoId = Guid.NewGuid() });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IniciarAquisicao_JogoValido_DeveRetornar202ComPedidoId()
    {
        var adminToken = JwtTestHelper.GerarTokenAdmin();
        using var adminClient = CriarClienteComToken(adminToken);

        var jogoResp = await adminClient.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-Aquisicao-{Guid.NewGuid()}", descricao = "desc", genero = "RPG", preco = 29.90m });
        jogoResp.EnsureSuccessStatusCode();
        var jogoId = (await jogoResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var usuarioId = Guid.NewGuid();
        using var userClient = CriarClienteComToken(JwtTestHelper.GerarToken(usuarioId));

        var resp = await userClient.PostAsJsonAsync("/api/biblioteca/aquisicoes", new { jogoId });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("pedidoId", out _).Should().BeTrue();
        body.TryGetProperty("mensagem", out _).Should().BeTrue();
    }

    [Fact]
    public async Task IniciarAquisicao_JogoInexistente_DeveRetornar404()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenUsuario());

        var resp = await client.PostAsJsonAsync("/api/biblioteca/aquisicoes",
            new { jogoId = Guid.NewGuid() });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IniciarAquisicao_MesmoJogoDuasVezes_DeveRetornar409NaSegunda()
    {
        var adminToken = JwtTestHelper.GerarTokenAdmin();
        using var adminClient = CriarClienteComToken(adminToken);

        var jogoResp = await adminClient.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-Dup-{Guid.NewGuid()}", descricao = "desc", genero = "RPG", preco = 10m });
        jogoResp.EnsureSuccessStatusCode();
        var jogoId = (await jogoResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var usuarioId = Guid.NewGuid();
        using var userClient = CriarClienteComToken(JwtTestHelper.GerarToken(usuarioId));

        var primeira = await userClient.PostAsJsonAsync("/api/biblioteca/aquisicoes", new { jogoId });
        primeira.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Segunda aquisição do mesmo jogo pelo mesmo usuário — pedido pendente
        var segunda = await userClient.PostAsJsonAsync("/api/biblioteca/aquisicoes", new { jogoId });
        segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task IniciarAquisicao_VariosJogosDiferentes_DeveRetornar202ParaCadaUm()
    {
        var adminToken = JwtTestHelper.GerarTokenAdmin();
        using var adminClient = CriarClienteComToken(adminToken);

        var jogoIds = new List<Guid>();
        for (var i = 1; i <= 3; i++)
        {
            var jogoResp = await adminClient.PostAsJsonAsync("/api/jogos",
                new { titulo = $"Jogo-Multi-{Guid.NewGuid()}", descricao = $"desc {i}", genero = "RPG", preco = i * 10m });
            jogoResp.EnsureSuccessStatusCode();
            var id = (await jogoResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            jogoIds.Add(id);
        }

        var usuarioId = Guid.NewGuid();
        using var userClient = CriarClienteComToken(JwtTestHelper.GerarToken(usuarioId));

        foreach (var jogoId in jogoIds)
        {
            var resp = await userClient.PostAsJsonAsync("/api/biblioteca/aquisicoes", new { jogoId });
            resp.StatusCode.Should().Be(HttpStatusCode.Accepted,
                because: $"jogo {jogoId} deveria ser aceito para aquisição");
        }
    }

    [Fact]
    public async Task ListarBiblioteca_UsuarioSemItens_DeveRetornar200ComListaVazia()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarToken(Guid.NewGuid()));

        var resp = await client.GetAsync("/api/biblioteca");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Utilitários ────────────────────────────────────────────────────

    private HttpClient CriarClienteComToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
