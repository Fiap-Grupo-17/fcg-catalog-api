using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Infrastructure.Testing;

/// <summary>
/// Índice de busca in-memory para o ambiente "Testing" (a suíte não exige Elasticsearch).
/// Imita o comportamento do Elasticsearch de forma simplificada: tolerância a erros de digitação
/// com a mesma regra do "fuzziness: AUTO" (distância de edição) e pesos título 3 / gênero 2 / descrição 1.
/// </summary>
public sealed class InMemoryJogoSearchIndex : IJogoSearchIndex
{
    private readonly ConcurrentDictionary<Guid, Jogo> _docs = new();

    public Task IndexarAsync(Jogo jogo, CancellationToken ct)
    {
        _docs[jogo.Id] = jogo;
        return Task.CompletedTask;
    }

    public Task RemoverAsync(Guid jogoId, CancellationToken ct)
    {
        _docs.TryRemove(jogoId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<JogoBuscaDto>> BuscarAsync(string termo, int tamanho, CancellationToken ct)
    {
        var termos = Tokens(termo);
        var resultado = _docs.Values
            .Select(j => (Jogo: j, Score: Pontuar(termos, j)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(tamanho)
            .Select(x => new JogoBuscaDto(x.Jogo.Id, x.Jogo.Titulo, x.Jogo.Descricao, x.Jogo.Genero, x.Jogo.Preco, x.Score))
            .ToList();
        return Task.FromResult<IReadOnlyList<JogoBuscaDto>>(resultado);
    }

    private static double Pontuar(IReadOnlyList<string> termos, Jogo j)
    {
        var titulo = Tokens(j.Titulo);
        var genero = Tokens(j.Genero);
        var descricao = Tokens(j.Descricao);
        double score = 0;
        foreach (var t in termos)
        {
            if (titulo.Any(p => Parecido(t, p))) score += 3;
            if (genero.Any(p => Parecido(t, p))) score += 2;
            if (descricao.Any(p => Parecido(t, p))) score += 1;
        }
        return score;
    }

    /// <summary>Mesma regra do fuzziness AUTO: 0 erros até 2 letras, 1 erro de 3 a 5, 2 erros a partir de 6.</summary>
    private static bool Parecido(string termo, string palavra)
    {
        var maxErros = termo.Length <= 2 ? 0 : termo.Length <= 5 ? 1 : 2;
        return Distancia(termo, palavra) <= maxErros;
    }

    private static int Distancia(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var k = 0; k <= b.Length; k++) d[0, k] = k;
        for (var i = 1; i <= a.Length; i++)
        for (var k = 1; k <= b.Length; k++)
        {
            var custo = a[i - 1] == b[k - 1] ? 0 : 1;
            d[i, k] = Math.Min(Math.Min(d[i - 1, k] + 1, d[i, k - 1] + 1), d[i - 1, k - 1] + custo);
            // transposição de letras vizinhas conta como 1 erro (como no Elasticsearch)
            if (i > 1 && k > 1 && a[i - 1] == b[k - 2] && a[i - 2] == b[k - 1])
                d[i, k] = Math.Min(d[i, k], d[i - 2, k - 2] + 1);
        }
        return d[a.Length, b.Length];
    }

    private static List<string> Tokens(string texto) =>
        SemAcento(texto.ToLowerInvariant())
            .Split(' ', ',', '.', ';', ':', '-', '!', '?', '/', '(', ')')
            .Where(t => t.Length > 0)
            .ToList();

    private static string SemAcento(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
