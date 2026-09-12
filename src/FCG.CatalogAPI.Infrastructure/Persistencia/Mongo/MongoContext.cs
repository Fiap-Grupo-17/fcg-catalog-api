using FCG.CatalogAPI.Application.Loja.CatalogoEstendido;
using FCG.CatalogAPI.Infrastructure.Persistencia.Mongo.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace FCG.CatalogAPI.Infrastructure.Persistencia.Mongo;

/// <summary>
/// Encapsula a conexão MongoDB e expõe as coleções tipadas do serviço de catálogo.
/// Registrado como singleton. O mapeamento BSON do read model é configurado uma
/// única vez por processo.
/// </summary>
public class MongoContext
{
    public const string GameCatalogExtendedCollection = "game_catalog_extended";
    public const string ProcessedEventsCollection = "processed_events";

    private static readonly object _mapLock = new();
    private static bool _mapped;

    public MongoOptions Options { get; }
    public IMongoDatabase Database { get; }

    public MongoContext(MongoOptions options)
    {
        Options = options;
        EnsureClassMaps();

        var client = new MongoClient(options.ConnectionString);
        Database = client.GetDatabase(options.Database);
    }

    public IMongoCollection<GameExtendedReadModel> GameCatalogExtended =>
        Database.GetCollection<GameExtendedReadModel>(GameCatalogExtendedCollection);

    public IMongoCollection<ProcessedEventDocument> ProcessedEvents =>
        Database.GetCollection<ProcessedEventDocument>(ProcessedEventsCollection);

    /// <summary>
    /// Mapeia <see cref="GameExtendedReadModel"/>: <c>GameId</c> vira o <c>_id</c>
    /// (representado como string GUID legível) e os elementos usam camelCase.
    /// </summary>
    private static void EnsureClassMaps()
    {
        if (_mapped) return;
        lock (_mapLock)
        {
            if (_mapped) return;

            var pack = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register(
                "fcgCatalogoEstendido",
                pack,
                t => t.FullName != null &&
                     t.FullName.StartsWith("FCG.CatalogAPI.Application.Loja.CatalogoEstendido"));

            if (!BsonClassMap.IsClassMapRegistered(typeof(GameExtendedReadModel)))
            {
                BsonClassMap.RegisterClassMap<GameExtendedReadModel>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(x => x.GameId)
                      .SetSerializer(new MongoDB.Bson.Serialization.Serializers.GuidSerializer(BsonType.String));
                });
            }

            _mapped = true;
        }
    }
}
