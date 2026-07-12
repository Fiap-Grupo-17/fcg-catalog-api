namespace FCG.CatalogAPI.Domain.Loja.Entidades;

public class Promocao
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public decimal PercentualDesconto { get; private set; }
    public DateTime Inicio { get; private set; }
    public DateTime Fim { get; private set; }
    public bool Ativa { get; private set; }
    public DateTime CriadaEm { get; private set; }

    private List<Guid> _jogosIds = new();
    public IReadOnlyList<Guid> JogosIds => _jogosIds.AsReadOnly();

    protected Promocao() { }

    public static Promocao Criar(string nome, decimal percentualDesconto,
        DateTime inicio, DateTime fim, IEnumerable<Guid> jogosIds)
    {
        ValidarParametros(nome, percentualDesconto, inicio, fim, jogosIds);

        return new Promocao
        {
            Id = Guid.NewGuid(),
            Nome = nome.Trim(),
            PercentualDesconto = percentualDesconto,
            Inicio = inicio,
            Fim = fim,
            Ativa = true,
            CriadaEm = DateTime.UtcNow,
            _jogosIds = jogosIds.ToList()
        };
    }

    public void Atualizar(string nome, decimal percentualDesconto,
        DateTime inicio, DateTime fim, IEnumerable<Guid> jogosIds)
    {
        ValidarParametros(nome, percentualDesconto, inicio, fim, jogosIds);
        Nome = nome.Trim();
        PercentualDesconto = percentualDesconto;
        Inicio = inicio;
        Fim = fim;
        _jogosIds.Clear();
        _jogosIds.AddRange(jogosIds);
    }

    public void Encerrar()
    {
        Ativa = false;
    }

    public bool EstaVigenteEm(DateTime data) =>
        Ativa && data >= Inicio && data <= Fim;

    public decimal AplicarDesconto(decimal preco) =>
        Math.Round(preco * (1 - PercentualDesconto / 100m), 2);

    public bool ContemJogo(Guid jogoId) => _jogosIds.Contains(jogoId);

    private static void ValidarParametros(string nome, decimal percentual,
        DateTime inicio, DateTime fim, IEnumerable<Guid> jogosIds)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome da promoção é obrigatório.");
        if (percentual <= 0 || percentual > 100)
            throw new ArgumentException("Percentual de desconto deve ser entre 1 e 100.");
        if (fim <= inicio)
            throw new ArgumentException("A data de fim deve ser posterior à data de início.");
        var ids = jogosIds?.ToList() ?? new List<Guid>();
        if (ids.Count == 0)
            throw new ArgumentException("A promoção deve conter ao menos um jogo.");
    }
}
