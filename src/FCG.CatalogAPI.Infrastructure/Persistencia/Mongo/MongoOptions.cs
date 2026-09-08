namespace FCG.CatalogAPI.Infrastructure.Persistencia.Mongo;

/// <summary>
/// Configuração da conexão MongoDB. Bind a partir da seção <c>Mongo</c> do appsettings.
/// </summary>
public class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string Database { get; set; } = "fcg_catalog";

    /// <summary>
    /// TTL (em dias) para a coleção <c>processed_events</c>. 0 = sem expiração
    /// (mantém histórico completo para auditoria). Decisão configurável do Raul.
    /// </summary>
    public int ProcessedEventsTtlDays { get; set; } = 0;
}
