using System.Text.Json.Serialization;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Infrastructure.Busca;

/// <summary>Documento do índice "jogos" (cópia de leitura do jogo, só para busca).</summary>
public class JogoDocumento
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = string.Empty;
    [JsonPropertyName("descricao")] public string Descricao { get; set; } = string.Empty;
    [JsonPropertyName("genero")] public string Genero { get; set; } = string.Empty;
    [JsonPropertyName("preco")] public decimal Preco { get; set; }
    [JsonPropertyName("indexadoEm")] public DateTime IndexadoEm { get; set; }

    public static JogoDocumento De(Jogo jogo) => new()
    {
        Id = jogo.Id,
        Titulo = jogo.Titulo,
        Descricao = jogo.Descricao,
        Genero = jogo.Genero,
        Preco = jogo.Preco,
        IndexadoEm = DateTime.UtcNow
    };
}
