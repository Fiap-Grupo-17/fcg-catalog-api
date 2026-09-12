using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.CatalogoEstendido;
using MongoDB.Driver;

namespace FCG.CatalogAPI.Infrastructure.Persistencia.Mongo;

/// <summary>
/// Repositório do read model "catálogo expandido" sobre MongoDB.
/// </summary>
public class MongoGameReadModelRepository : IGameReadModelRepository
{
    private readonly IMongoCollection<GameExtendedReadModel> _collection;

    public MongoGameReadModelRepository(MongoContext ctx) => _collection = ctx.GameCatalogExtended;

    public async Task<GameExtendedReadModel?> GetByGameIdAsync(Guid gameId, CancellationToken ct)
    {
        var filter = Builders<GameExtendedReadModel>.Filter.Eq(x => x.GameId, gameId);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task UpsertAsync(GameExtendedReadModel doc, CancellationToken ct)
    {
        doc.AtualizadoEm = DateTime.UtcNow;
        var filter = Builders<GameExtendedReadModel>.Filter.Eq(x => x.GameId, doc.GameId);
        await _collection.ReplaceOneAsync(
            filter, doc, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task<IReadOnlyList<GameExtendedReadModel>> ListByTagAsync(string tag, CancellationToken ct)
    {
        var filter = Builders<GameExtendedReadModel>.Filter.AnyEq(x => x.Tags, tag);
        return await _collection.Find(filter).ToListAsync(ct);
    }
}
