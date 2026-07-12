using System.Net.Http.Headers;
using FCG.CatalogAPI.IntegrationTests.Infrastructure;

namespace FCG.CatalogAPI.IntegrationTests.Loja;

public class JogoEndpointTests : IClassFixture<FcgCatalogWebApplicationFactory>
{
    private readonly FcgCatalogWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public JogoEndpointTests(FcgCatalogWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── LISTAR ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListarJogos_SemAuth_DeveRetornar200()
    {
        var resp = await _client.GetAsync("/api/jogos");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── CADASTRAR ──────────────────────────────────────────────────────

    [Fact]
    public async Task CadastrarJogo_SemAuth_DeveRetornar401()
    {
        var resp = await _client.PostAsJsonAsync("/api/jogos",
            new { titulo = "Teste", descricao = "desc", genero = "RPG", preco = 10m });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CadastrarJogo_UsuarioComum_DeveRetornar403()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenUsuario());

        var resp = await client.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-{Guid.NewGuid()}", descricao = "desc", genero = "RPG", preco = 10m });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CadastrarJogo_Admin_DeveRetornar201()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var resp = await client.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-Admin-{Guid.NewGuid()}", descricao = "desc", genero = "Aventura", preco = 99.90m });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("preco").GetDecimal().Should().Be(99.90m);
        body.TryGetProperty("id", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public async Task CadastrarJogo_PrecoNegativo_DeveRetornar400(decimal preco)
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var resp = await client.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-{Guid.NewGuid()}", descricao = "desc", genero = "RPG", preco });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CadastrarJogo_PrecoZero_DeveRetornar201()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var resp = await client.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-Gratis-{Guid.NewGuid()}", descricao = "desc", genero = "Casual", preco = 0m });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("preco").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task CadastrarJogo_TituloVazio_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var resp = await client.PostAsJsonAsync("/api/jogos",
            new { titulo = "", descricao = "desc", genero = "RPG", preco = 10m });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── ATUALIZAR ──────────────────────────────────────────────────────

    [Fact]
    public async Task AtualizarJogo_SemAuth_DeveRetornar401()
    {
        var resp = await _client.PutAsJsonAsync($"/api/jogos/{Guid.NewGuid()}",
            new { titulo = "Novo", descricao = "desc", genero = "RPG", preco = 10m });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AtualizarJogo_UsuarioComum_DeveRetornar403()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenUsuario());

        var resp = await client.PutAsJsonAsync($"/api/jogos/{Guid.NewGuid()}",
            new { titulo = "Novo Nome", descricao = "desc", genero = "RPG", preco = 10m });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AtualizarJogo_IdInexistente_DeveRetornar404()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var resp = await client.PutAsJsonAsync($"/api/jogos/{Guid.NewGuid()}",
            new { titulo = "Nome Qualquer", descricao = "desc", genero = "RPG", preco = 10m });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AtualizarJogo_TodosOsCampos_DeveRetornar200()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var criarResp = await client.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-Full-{Guid.NewGuid()}", descricao = "desc original", genero = "RPG", preco = 30m });
        criarResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var jogoId = (await criarResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await client.PutAsJsonAsync($"/api/jogos/{jogoId}",
            new { titulo = $"Jogo-Atualizado-{Guid.NewGuid()}", descricao = "desc nova", genero = "Aventura", preco = 99m });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("preco").GetDecimal().Should().Be(99m);
    }

    [Fact]
    public async Task AtualizarJogo_TituloVazio_DeveRetornar400()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var criarResp = await client.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-Titulo-{Guid.NewGuid()}", descricao = "desc", genero = "RPG", preco = 50m });
        criarResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var jogoId = (await criarResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await client.PutAsJsonAsync($"/api/jogos/{jogoId}",
            new { titulo = "", descricao = "desc", genero = "RPG", preco = 50m });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── DESATIVAR / ATIVAR ─────────────────────────────────────────────

    [Fact]
    public async Task DesativarJogo_SemAuth_DeveRetornar401()
    {
        var resp = await _client.PatchAsync($"/api/jogos/{Guid.NewGuid()}/desativar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DesativarJogo_UsuarioComum_DeveRetornar403()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenUsuario());
        var resp = await client.PatchAsync($"/api/jogos/{Guid.NewGuid()}/desativar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DesativarJogo_IdInexistente_DeveRetornar404()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var resp = await client.PatchAsync($"/api/jogos/{Guid.NewGuid()}/desativar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DesativarJogo_Admin_DeveRetornar204()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var criarResp = await client.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-Desat-{Guid.NewGuid()}", descricao = "desc", genero = "RPG", preco = 50m });
        criarResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var jogoId = (await criarResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await client.PatchAsync($"/api/jogos/{jogoId}/desativar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AtivarJogo_Admin_DeveRetornar204()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());

        var criarResp = await client.PostAsJsonAsync("/api/jogos",
            new { titulo = $"Jogo-Ativar-{Guid.NewGuid()}", descricao = "desc", genero = "RPG", preco = 50m });
        criarResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var jogoId = (await criarResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Desativar primeiro
        await client.PatchAsync($"/api/jogos/{jogoId}/desativar", null);

        // Reativar
        var resp = await client.PatchAsync($"/api/jogos/{jogoId}/ativar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AtivarJogo_IdInexistente_DeveRetornar404()
    {
        using var client = CriarClienteComToken(JwtTestHelper.GerarTokenAdmin());
        var resp = await client.PatchAsync($"/api/jogos/{Guid.NewGuid()}/ativar", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
