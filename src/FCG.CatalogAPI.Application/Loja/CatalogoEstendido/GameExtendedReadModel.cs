namespace FCG.CatalogAPI.Application.Loja.CatalogoEstendido;

/// <summary>
/// Read model do "catálogo expandido" persistido no MongoDB (coleção
/// <c>game_catalog_extended</c>). Guarda metadados ricos e flexíveis do jogo
/// (mídia, screenshots, tags, requisitos) — dados NOVOS, próprios da Fase 3.
/// O jogo-base (id, título, preço) continua no PostgreSQL como fonte de verdade;
/// os campos snapshot aqui são meramente informativos para exibição.
/// </summary>
public class GameExtendedReadModel
{
    /// <summary>Id do jogo — igual ao <c>Jogo.Id</c> do Postgres. Vira <c>_id</c> no Mongo.</summary>
    public Guid GameId { get; set; }

    // Snapshot (fonte de verdade é o Postgres)
    public string Titulo { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public decimal Preco { get; set; }

    // Metadados ricos (exclusivos do read model)
    public MidiaInfo Midia { get; set; } = new();
    public List<string> Screenshots { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public RequisitosInfo Requisitos { get; set; } = new();

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Origem do documento: "admin" (escrita explícita) ou "lazy-seed".</summary>
    public string Origem { get; set; } = "admin";
}

public class MidiaInfo
{
    public string? CoverUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? TrailerUrl { get; set; }
}

public class RequisitosInfo
{
    public RequisitoSistema? Minimo { get; set; }
    public RequisitoSistema? Recomendado { get; set; }
}

public class RequisitoSistema
{
    public string? So { get; set; }
    public string? Cpu { get; set; }
    public string? Gpu { get; set; }
    public string? Ram { get; set; }
}
