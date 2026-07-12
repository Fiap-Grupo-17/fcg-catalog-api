using System.Net.Http.Headers;
using FCG.CatalogAPI.IntegrationTests.Infrastructure;

namespace FCG.CatalogAPI.IntegrationTests.Loja;

public class PromocaoEndpointTests : IClassFixture<FcgCatalogWebApplicationFactory>
{
    private readonly FcgCatalogWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PromocaoEndpointTests(FcgCatalogWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── LISTAR PROMOÇÕES ───────────────────────────────────────────────

    [Fact]
    public async Task ListarPromocoes_SemAutenticacao_DeveRetornar200()
    {
        var resp = await _client.GetAsync("/api/promocoes");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ListarPromocoesAtivas_SemAutenticacao_DeveRetornar200()
    {
        var resp = await _client.GetAsync("/api/promocoes?ativas=true");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListarPromocoesVigentes_Admin_DeveRetornar200()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var resp = await client.GetAsync("/api/promocoes?vigentes=true");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ListarPromocoesAtivas_Admin_DeveRetornar200()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var resp = await client.GetAsync("/api/promocoes?ativas=true");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ListarPromocoesVigentesEAtivas_Admin_DeveRetornar200()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var resp = await client.GetAsync("/api/promocoes?ativas=true&vigentes=true");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── CRIAR PROMOÇÃO ─────────────────────────────────────────────────

    [Fact]
    public async Task CriarPromocao_SemAutenticacao_DeveRetornar401()
    {
        var req = new
        {
            nome = "Promo Teste",
            percentualDesconto = 10m,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(7),
            jogosIds = new[] { Guid.NewGuid() }
        };

        var resp = await _client.PostAsJsonAsync("/api/promocoes", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CriarPromocao_UsuarioComum_DeveRetornar403()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenUsuario());

        var req = new
        {
            nome = "Promo Teste",
            percentualDesconto = 10m,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(7),
            jogosIds = new[] { Guid.NewGuid() }
        };

        var resp = await client.PostAsJsonAsync("/api/promocoes", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CriarPromocao_Admin_DeveRetornar201()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var req = new
        {
            nome = $"Promo-{Guid.NewGuid()}",
            percentualDesconto = 20m,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(7),
            jogosIds = new[] { jogoId }
        };

        var resp = await client.PostAsJsonAsync("/api/promocoes", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("percentualDesconto").GetDecimal().Should().Be(20m);
        body.GetProperty("ativa").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CriarPromocao_JogoInexistente_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var req = new
        {
            nome = "Promo Teste",
            percentualDesconto = 10m,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(7),
            jogosIds = new[] { Guid.NewGuid() }
        };

        var resp = await client.PostAsJsonAsync("/api/promocoes", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CriarPromocao_PercentualZero_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var req = new
        {
            nome = "Promo Teste",
            percentualDesconto = 0m,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(7),
            jogosIds = new[] { jogoId }
        };

        var resp = await client.PostAsJsonAsync("/api/promocoes", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── ENCERRAR PROMOÇÃO ──────────────────────────────────────────────

    [Fact]
    public async Task EncerrarPromocao_SemAutenticacao_DeveRetornar401()
    {
        var resp = await _client.PatchAsync($"/api/promocoes/{Guid.NewGuid()}/encerrar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EncerrarPromocao_UsuarioComum_DeveRetornar403()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenUsuario());
        var resp = await client.PatchAsync($"/api/promocoes/{Guid.NewGuid()}/encerrar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EncerrarPromocao_IdInvalido_DeveRetornar404()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var resp = await client.PatchAsync($"/api/promocoes/{Guid.NewGuid()}/encerrar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EncerrarPromocao_Admin_DeveRetornar204()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var criarResp = await client.PostAsJsonAsync("/api/promocoes", new
        {
            nome = $"Promo-Encerrar-{Guid.NewGuid()}",
            percentualDesconto = 15m,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(5),
            jogosIds = new[] { jogoId }
        });
        criarResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var promoId = (await criarResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await client.PatchAsync($"/api/promocoes/{promoId}/encerrar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── ATUALIZAR PROMOÇÃO ─────────────────────────────────────────────

    [Fact]
    public async Task AtualizarPromocao_SemAutenticacao_DeveRetornar401()
    {
        var resp = await _client.PutAsJsonAsync($"/api/promocoes/{Guid.NewGuid()}",
            new { nome = "X", percentualDesconto = 10, inicio = DateTime.UtcNow, fim = DateTime.UtcNow.AddDays(1), jogosIds = new[] { Guid.NewGuid() } });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AtualizarPromocao_UsuarioComum_DeveRetornar403()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenUsuario());
        var resp = await client.PutAsJsonAsync($"/api/promocoes/{Guid.NewGuid()}",
            new { nome = "X", percentualDesconto = 10, inicio = DateTime.UtcNow, fim = DateTime.UtcNow.AddDays(1), jogosIds = new[] { Guid.NewGuid() } });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AtualizarPromocao_IdInexistente_DeveRetornar404()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var resp = await client.PutAsJsonAsync($"/api/promocoes/{Guid.NewGuid()}",
            new { nome = "Promo", percentualDesconto = 10, inicio = DateTime.UtcNow, fim = DateTime.UtcNow.AddDays(1), jogosIds = new[] { jogoId } });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AtualizarPromocao_ComDadosValidos_DeveRetornar200ComDadosAtualizados()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var criarResp = await client.PostAsJsonAsync("/api/promocoes", new
        {
            nome = "Promo Original",
            percentualDesconto = 10,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(5),
            jogosIds = new[] { jogoId }
        });
        criarResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var promoId = (await criarResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var atualizarResp = await client.PutAsJsonAsync($"/api/promocoes/{promoId}", new
        {
            nome = "Promo Atualizada",
            percentualDesconto = 35,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(15),
            jogosIds = new[] { jogoId }
        });

        atualizarResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await atualizarResp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("nome").GetString().Should().Be("Promo Atualizada");
        body.GetProperty("percentualDesconto").GetDecimal().Should().Be(35m);
    }

    [Fact]
    public async Task AtualizarPromocao_NomeVazio_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var resp = await client.PutAsJsonAsync($"/api/promocoes/{Guid.NewGuid()}", new
        {
            nome = "",
            percentualDesconto = 10,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(5),
            jogosIds = new[] { jogoId }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AtualizarPromocao_PercentualZero_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var resp = await client.PutAsJsonAsync($"/api/promocoes/{Guid.NewGuid()}", new
        {
            nome = "Promo",
            percentualDesconto = 0,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(5),
            jogosIds = new[] { jogoId }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AtualizarPromocao_PercentualAcimaDe100_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var resp = await client.PutAsJsonAsync($"/api/promocoes/{Guid.NewGuid()}", new
        {
            nome = "Promo",
            percentualDesconto = 101,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(5),
            jogosIds = new[] { jogoId }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AtualizarPromocao_DataFimAnteriorAoInicio_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var resp = await client.PutAsJsonAsync($"/api/promocoes/{Guid.NewGuid()}", new
        {
            nome = "Promo",
            percentualDesconto = 10,
            inicio = DateTime.UtcNow.AddDays(10),
            fim = DateTime.UtcNow.AddDays(1),
            jogosIds = new[] { jogoId }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AtualizarPromocao_SemJogos_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var resp = await client.PutAsJsonAsync($"/api/promocoes/{Guid.NewGuid()}", new
        {
            nome = "Promo",
            percentualDesconto = 10,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(5),
            jogosIds = Array.Empty<Guid>()
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AtualizarPromocao_ComJogoInexistente_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var jogoId = await CriarJogoAsync(client);

        var criarResp = await client.PostAsJsonAsync("/api/promocoes", new
        {
            nome = "Promo Para Atualizar",
            percentualDesconto = 10,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(5),
            jogosIds = new[] { jogoId }
        });
        criarResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var promoId = (await criarResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await client.PutAsJsonAsync($"/api/promocoes/{promoId}", new
        {
            nome = "Promo Atualizada",
            percentualDesconto = 20,
            inicio = DateTime.UtcNow,
            fim = DateTime.UtcNow.AddDays(5),
            jogosIds = new[] { Guid.NewGuid() }  // jogo inexistente
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Utilitários ────────────────────────────────────────────────────

    private async Task<Guid> CriarJogoAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/jogos", new
        {
            titulo = $"Jogo-Promo-{Guid.NewGuid()}",
            descricao = "Jogo para teste de promoção",
            genero = "RPG",
            preco = 100m
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private HttpClient CriarClienteComToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
