namespace FCG.CatalogAPI.Domain.Biblioteca.Entidades;

public class ItemBiblioteca
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid JogoId { get; private set; }
    public string JogoTitulo { get; private set; } = string.Empty;
    public Guid PedidoId { get; private set; }
    public DateTime AdquiridoEm { get; private set; }

    protected ItemBiblioteca() { }

    public static ItemBiblioteca Criar(Guid usuarioId, Guid jogoId, string jogoTitulo, Guid pedidoId)
        => new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            JogoId = jogoId,
            JogoTitulo = jogoTitulo,
            PedidoId = pedidoId,
            AdquiridoEm = DateTime.UtcNow
        };
}
