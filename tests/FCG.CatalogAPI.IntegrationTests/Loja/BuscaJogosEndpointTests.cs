using System.Net.Http.Headers;
using FCG.CatalogAPI.IntegrationTests.Infrastructure;

namespace FCG.CatalogAPI.IntegrationTests.Loja;

/// <summary>GET /api/jogos/search — busca avançada (TC4 item 3), com índice in-memory no ambiente Testing.</summary>
public class BuscaJogosEndpointTests : IClassFixture<FcgCatalogWebApplicationFactory>
{
    private readonly FcgCatalogWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BuscaJogosEndpointTests(FcgCatalogWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private HttpClient ClienteAdmin()
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GerarTokenAdmin());
        return c;
    }

    [Fact]
    public async Task Search_SemTermo_DeveRetornar400()
    {
        var resp = await _client.GetAsync("/api/jogos/search");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_SemAuth_DeveSerPublico()
    {
        var resp = await _client.GetAsync("/api/jogos/search?q=qualquer");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CadastrarEditarDesativar_DeveSincronizarOIndice()
    {
        using var admin = ClienteAdmin();
        var sufixo = Guid.NewGuid().ToString("N")[..6];
        var titulo = $"Xadrezmania{sufixo}";

        // inserção → indexado
        var criado = await admin.PostAsJsonAsync("/api/jogos",
            new { titulo, descricao = "Jogo de tabuleiro", genero = "Estrategia", preco = 19.90m });
        criado.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var achou = await _client.GetFromJsonAsync<JsonElement>($"/api/jogos/search?q={titulo}");
        achou.EnumerateArray().Should().Contain(e => e.GetProperty("id").GetGuid() == id);

        // edição → índice atualizado com o título novo
        var novoTitulo = $"Damasmania{sufixo}";
        var editado = await admin.PutAsJsonAsync($"/api/jogos/{id}",
            new { titulo = novoTitulo, descricao = "Jogo de tabuleiro", genero = "Estrategia", preco = 19.90m });
        editado.StatusCode.Should().Be(HttpStatusCode.OK);

        var porNovo = await _client.GetFromJsonAsync<JsonElement>($"/api/jogos/search?q={novoTitulo}");
        porNovo.EnumerateArray().Should().Contain(e => e.GetProperty("titulo").GetString() == novoTitulo);

        // desativação → sai do índice
        (await admin.PatchAsync($"/api/jogos/{id}/desativar", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var depois = await _client.GetFromJsonAsync<JsonElement>($"/api/jogos/search?q={novoTitulo}");
        depois.EnumerateArray().Should().NotContain(e => e.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task Search_ComErroDeDigitacao_DeveRetornarScore()
    {
        using var admin = ClienteAdmin();
        var titulo = $"Labirinto Quantico {Guid.NewGuid():N}"[..28];
        (await admin.PostAsJsonAsync("/api/jogos",
            new { titulo, descricao = "Puzzle", genero = "Puzzle", preco = 9.90m })).StatusCode.Should().Be(HttpStatusCode.Created);

        var resultado = await _client.GetFromJsonAsync<JsonElement>("/api/jogos/search?q=labirnito");

        var hit = resultado.EnumerateArray().First(e => e.GetProperty("titulo").GetString() == titulo);
        hit.GetProperty("score").GetDouble().Should().BeGreaterThan(0);
    }
}
