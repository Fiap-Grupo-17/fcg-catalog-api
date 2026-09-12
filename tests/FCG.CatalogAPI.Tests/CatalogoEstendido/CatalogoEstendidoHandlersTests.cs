using System.Collections.Concurrent;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.CatalogoEstendido;
using FCG.CatalogAPI.Application.Loja.CatalogoEstendido.Commands;
using FCG.CatalogAPI.Application.Loja.CatalogoEstendido.Queries;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using FCG.CatalogAPI.Tests.Application;
using FluentAssertions;

namespace FCG.CatalogAPI.Tests.CatalogoEstendido;

/// <summary>
/// Fake in-memory de <see cref="IGameReadModelRepository"/> — o projeto de testes não
/// referencia a Infrastructure, então este fake local substitui o Mongo real.
/// </summary>
public sealed class FakeGameReadModelRepository : IGameReadModelRepository
{
    private readonly ConcurrentDictionary<Guid, GameExtendedReadModel> _store = new();

    public Task<GameExtendedReadModel?> GetByGameIdAsync(Guid gameId, CancellationToken ct)
        => Task.FromResult(_store.TryGetValue(gameId, out var doc) ? doc : null);

    public Task UpsertAsync(GameExtendedReadModel doc, CancellationToken ct)
    {
        doc.AtualizadoEm = DateTime.UtcNow;
        _store[doc.GameId] = doc;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GameExtendedReadModel>> ListByTagAsync(string tag, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<GameExtendedReadModel>>(
            _store.Values.Where(d => d.Tags.Contains(tag)).ToList());

    public bool Contem(Guid gameId) => _store.ContainsKey(gameId);
}

public class CatalogoEstendidoHandlersTests
{
    private static async Task<TestCatalogDbContext> DbComJogo(Jogo jogo)
    {
        var db = TestCatalogDbContext.Criar();
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);
        return db;
    }

    // ---------- UpsertDetalhesJogoHandler ----------

    [Fact]
    public async Task Upsert_JogoExistente_DeveCriarDocumentoComSnapshot()
    {
        var jogo = Jogo.Criar("The Great Game", "desc", "RPG", 99.90m);
        using var db = await DbComJogo(jogo);
        var readModelRepo = new FakeGameReadModelRepository();
        var handler = new UpsertDetalhesJogoHandler(db, readModelRepo);

        var cmd = new UpsertDetalhesJogoCommand(
            new MidiaInfo { CoverUrl = "https://cdn/cover.png" },
            new List<string> { "https://cdn/s1.png" },
            new List<string> { "rpg", "aventura" },
            new RequisitosInfo { Minimo = new RequisitoSistema { So = "Windows 10" } });

        var resultado = await handler.HandleAsync(jogo.Id, cmd, CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.GameId.Should().Be(jogo.Id);
        resultado.Titulo.Should().Be("The Great Game");
        resultado.Genero.Should().Be("RPG");
        resultado.Preco.Should().Be(99.90m);
        resultado.Midia.CoverUrl.Should().Be("https://cdn/cover.png");
        resultado.Tags.Should().BeEquivalentTo("rpg", "aventura");
        resultado.Origem.Should().Be("admin");

        var persistido = await readModelRepo.GetByGameIdAsync(jogo.Id, CancellationToken.None);
        persistido.Should().NotBeNull();
        persistido!.Screenshots.Should().ContainSingle().Which.Should().Be("https://cdn/s1.png");
    }

    [Fact]
    public async Task Upsert_DocumentoJaExistente_DeveAtualizarValores()
    {
        var jogo = Jogo.Criar("Jogo Base", "desc", "Ação", 49.90m);
        using var db = await DbComJogo(jogo);
        var readModelRepo = new FakeGameReadModelRepository();
        var handler = new UpsertDetalhesJogoHandler(db, readModelRepo);

        await handler.HandleAsync(
            jogo.Id,
            new UpsertDetalhesJogoCommand(null, new List<string> { "antiga" }, null, null),
            CancellationToken.None);

        var resultado = await handler.HandleAsync(
            jogo.Id,
            new UpsertDetalhesJogoCommand(null, new List<string> { "nova" }, new List<string> { "ação" }, null),
            CancellationToken.None);

        resultado!.Screenshots.Should().BeEquivalentTo("nova");
        resultado.Tags.Should().BeEquivalentTo("ação");

        var persistido = await readModelRepo.GetByGameIdAsync(jogo.Id, CancellationToken.None);
        persistido!.Screenshots.Should().BeEquivalentTo("nova");
    }

    [Fact]
    public async Task Upsert_JogoBaseInexistente_DeveRetornarNull()
    {
        using var db = TestCatalogDbContext.Criar();
        var readModelRepo = new FakeGameReadModelRepository();
        var handler = new UpsertDetalhesJogoHandler(db, readModelRepo);

        var resultado = await handler.HandleAsync(
            Guid.NewGuid(),
            new UpsertDetalhesJogoCommand(null, null, null, null),
            CancellationToken.None);

        resultado.Should().BeNull();
    }

    // ---------- BuscarDetalhesJogoHandler ----------

    [Fact]
    public async Task Buscar_DocumentoExistente_DeveRetornarSemConsultarPostgres()
    {
        var jogo = Jogo.Criar("Jogo Com Detalhes", "desc", "Estratégia", 10m);
        using var db = await DbComJogo(jogo);
        var readModelRepo = new FakeGameReadModelRepository();
        await readModelRepo.UpsertAsync(new GameExtendedReadModel
        {
            GameId = jogo.Id,
            Titulo = jogo.Titulo,
            Genero = jogo.Genero,
            Preco = jogo.Preco,
            Tags = new List<string> { "estrategia" },
            Origem = "admin"
        }, CancellationToken.None);
        var handler = new BuscarDetalhesJogoHandler(db, readModelRepo);

        var resultado = await handler.HandleAsync(jogo.Id, CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.Origem.Should().Be("admin");
        resultado.Tags.Should().BeEquivalentTo("estrategia");
    }

    [Fact]
    public async Task Buscar_DocumentoAusenteJogoBaseExistente_DeveFazerLazySeed()
    {
        var jogo = Jogo.Criar("Jogo Sem Detalhes", "desc", "Puzzle", 5m);
        using var db = await DbComJogo(jogo);
        var readModelRepo = new FakeGameReadModelRepository();
        var handler = new BuscarDetalhesJogoHandler(db, readModelRepo);

        var resultado = await handler.HandleAsync(jogo.Id, CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.Origem.Should().Be("lazy-seed");
        resultado.Titulo.Should().Be("Jogo Sem Detalhes");
        resultado.Genero.Should().Be("Puzzle");
        resultado.Tags.Should().BeEmpty();
        readModelRepo.Contem(jogo.Id).Should().BeTrue();
    }

    [Fact]
    public async Task Buscar_JogoInexistente_DeveRetornarNull()
    {
        using var db = TestCatalogDbContext.Criar();
        var readModelRepo = new FakeGameReadModelRepository();
        var handler = new BuscarDetalhesJogoHandler(db, readModelRepo);

        var resultado = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        resultado.Should().BeNull();
    }
}
