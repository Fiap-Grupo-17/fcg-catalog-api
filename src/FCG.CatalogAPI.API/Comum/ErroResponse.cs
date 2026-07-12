namespace FCG.CatalogAPI.API.Comum;

/// <summary>Resposta padrão de erro com mensagem única.</summary>
/// <param name="Erro">Descrição do erro ocorrido.</param>
public record ErroResponse(string Erro);
