namespace FCG.CatalogAPI.Infrastructure.Busca;

/// <summary>
/// Configuração do Elasticsearch (seção "Elasticsearch"). Senha e API key NUNCA ficam no
/// appsettings: no AKS chegam por variável de ambiente vinda do Key Vault
/// (Elasticsearch__Password ← platform-secrets/ELASTIC_PASSWORD).
/// </summary>
public class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "jogos";

    /// <summary>Autenticação básica (Elasticsearch no AKS / docker-compose).</summary>
    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>Alternativa: API key (ex.: Elastic Cloud). Se preenchida, tem prioridade.</summary>
    public string? ApiKey { get; set; }
}
