namespace FCG.CatalogAPI.Domain.Loja.Entidades;

public enum StatusPedido { Pendente, Aprovado, Rejeitado }

public class Pedido
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid JogoId { get; private set; }
    public string JogoTitulo { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public StatusPedido Status { get; private set; }
    public string? TransacaoId { get; private set; }
    public string? MotivoRejeicao { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? ProcessadoEm { get; private set; }

    protected Pedido() { }

    public static Pedido Criar(Guid usuarioId, Guid jogoId, string jogoTitulo, decimal valor)
        => new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            JogoId = jogoId,
            JogoTitulo = jogoTitulo,
            Valor = valor,
            Status = StatusPedido.Pendente,
            CriadoEm = DateTime.UtcNow
        };

    public void MarcarAprovado(string transacaoId)
    {
        Status = StatusPedido.Aprovado;
        TransacaoId = transacaoId;
        ProcessadoEm = DateTime.UtcNow;
    }

    public void MarcarRejeitado(string motivo)
    {
        Status = StatusPedido.Rejeitado;
        MotivoRejeicao = motivo;
        ProcessadoEm = DateTime.UtcNow;
    }
}
