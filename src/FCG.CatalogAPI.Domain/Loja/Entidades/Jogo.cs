namespace FCG.CatalogAPI.Domain.Loja.Entidades;

public class Jogo
{
    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public string Genero { get; private set; } = string.Empty;
    public decimal Preco { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }

    protected Jogo() { }

    public static Jogo Criar(string titulo, string descricao, string genero, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new ArgumentException("Título é obrigatório.");
        if (preco < 0) throw new ArgumentException("Preço não pode ser negativo.");

        return new Jogo
        {
            Id = Guid.NewGuid(),
            Titulo = titulo,
            Descricao = descricao,
            Genero = genero,
            Preco = preco,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public void Atualizar(string titulo, string descricao, string genero, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new ArgumentException("Título é obrigatório.");
        if (preco < 0) throw new ArgumentException("Preço não pode ser negativo.");
        Titulo = titulo;
        Descricao = descricao;
        Genero = genero;
        Preco = preco;
    }

    public void Desativar() => Ativo = false;
    public void Ativar() => Ativo = true;
}
