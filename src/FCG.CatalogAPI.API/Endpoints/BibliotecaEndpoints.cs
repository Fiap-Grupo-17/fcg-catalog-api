using FCG.CatalogAPI.API.Comum;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Application.Loja.Queries;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FCG.CatalogAPI.API.Endpoints;

public static class BibliotecaEndpoints
{
    public static IEndpointRouteBuilder MapBibliotecaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/biblioteca").WithTags("Biblioteca").RequireAuthorization();

        // ── POST /api/biblioteca/aquisicoes ───────────────────────────────
        group.MapPost("/aquisicoes", async (
            AquisicaoRequest req,
            IniciarAquisicaoHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var usuarioIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");

            if (!Guid.TryParse(usuarioIdStr, out var usuarioId))
                return Results.Unauthorized();

            var result = await handler.HandleAsync(
                new IniciarAquisicaoCommand(usuarioId, req.JogoId), ct);

            return Results.Accepted($"/api/biblioteca/pedidos/{result.PedidoId}", result);
        })
        .WithName("IniciarAquisicao")
        .WithSummary("Adquirir jogo")
        .WithDescription(
            "Inicia o processo de aquisição de um jogo para o usuário autenticado. " +
            "O pagamento é processado de forma assíncrona. Retorna 202 com o ID do pedido para acompanhamento. " +
            "O jogo será adicionado à biblioteca após aprovação do pagamento.\n\n" +
            "**400** – JogoId ausente ou inválido.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**404** – jogo não encontrado ou inativo.\n\n" +
            "**409** – o usuário já possui este jogo na biblioteca ou há um pedido pendente para ele.")
        .Produces<IniciarAquisicaoResult>(202)
        .Produces<ErroResponse>(400)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(404)
        .Produces<ErroResponse>(409);

        // ── GET /api/biblioteca ───────────────────────────────────────────
        group.MapGet("/", async (
            ICatalogDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!Guid.TryParse(
                    user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"),
                    out var usuarioId))
                return Results.Unauthorized();

            var itens = await db.ItensBiblioteca
                .AsNoTracking()
                .Where(i => i.UsuarioId == usuarioId)
                .OrderBy(i => i.JogoTitulo)
                .Select(i => new { i.Id, i.JogoId, i.JogoTitulo, i.AdquiridoEm })
                .ToListAsync(ct);

            return Results.Ok(itens);
        })
        .WithName("MinhasBiblioteca")
        .WithSummary("Listar minha biblioteca")
        .WithDescription(
            "Retorna todos os jogos adquiridos pelo usuário autenticado, ordenados por título.\n\n" +
            "**401** – token ausente ou inválido.")
        .Produces(200)
        .Produces<ErroResponse>(401);

        // ── GET /api/biblioteca/pedidos ───────────────────────────────────
        group.MapGet("/pedidos", async (
            ListarPedidosHandler handler,
            ClaimsPrincipal user,
            string? status,
            Guid? usuarioId,
            int? pagina,
            int? tamanhoPagina,
            CancellationToken ct) =>
        {
            var callerId = Guid.TryParse(
                user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"),
                out var uid) ? uid : (Guid?)null;

            var isAdmin = user.IsInRole("Admin") || user.IsInRole("Administrador");

            // Usuário comum só vê os próprios pedidos
            var filtroUsuario = isAdmin ? usuarioId : callerId;

            var result = await handler.HandleAsync(
                filtroUsuario,
                status,
                pagina ?? 1,
                Math.Min(tamanhoPagina ?? 20, 100),
                ct);

            return Results.Ok(result);
        })
        .WithName("ListarPedidos")
        .WithSummary("Listar pedidos")
        .WithDescription(
            "Retorna a lista paginada de pedidos de aquisição.\n\n" +
            "**Usuário comum** – vê apenas seus próprios pedidos (filtro de `usuarioId` é ignorado).\n\n" +
            "**Administrador** – pode filtrar por qualquer `usuarioId` ou listar todos.\n\n" +
            "Filtros opcionais: `status` (Pendente | Aprovado | Rejeitado), `usuarioId`, `pagina`, `tamanhoPagina` (máx. 100).\n\n" +
            "Resultados ordenados por data de criação decrescente.\n\n" +
            "**401** – token ausente ou inválido.")
        .Produces<ListarPedidosResult>(200)
        .Produces<ErroResponse>(401);

        // ── GET /api/biblioteca/pedidos/{id} ──────────────────────────────
        group.MapGet("/pedidos/{id:guid}", async (
            Guid id,
            BuscarPedidoHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var pedido = await handler.HandleAsync(id, ct);
            if (pedido is null)
                return Results.NotFound(new ErroResponse("Pedido não encontrado."));

            var usuarioIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            var isAdmin = user.IsInRole("Admin") || user.IsInRole("Administrador");

            if (!isAdmin && Guid.TryParse(usuarioIdStr, out var usuarioId) && pedido.UsuarioId != usuarioId)
                return Results.Forbid();

            return Results.Ok(pedido);
        })
        .WithName("BuscarPedido")
        .WithSummary("Consultar pedido por ID")
        .WithDescription(
            "Retorna os detalhes de um pedido específico pelo seu GUID. " +
            "Usuários comuns só visualizam seus próprios pedidos; Administradores visualizam qualquer pedido.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – pedido pertence a outro usuário.\n\n" +
            "**404** – pedido não encontrado.")
        .Produces<PedidoDto>(200)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403)
        .Produces<ErroResponse>(404);

        // ── POST /api/biblioteca/pedidos/{id}/reprocessar ─────────────────
        group.MapPost("/pedidos/{id:guid}/reprocessar", async (
            Guid id,
            ReprocessarPedidoHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var usuarioIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");

            if (!Guid.TryParse(usuarioIdStr, out var usuarioId))
                return Results.Unauthorized();

            var isAdmin = user.IsInRole("Admin") || user.IsInRole("Administrador");

            var result = await handler.HandleAsync(id, usuarioId, isAdmin, ct);
            return Results.Accepted($"/api/biblioteca/pedidos/{id}", result);
        })
        .WithName("ReprocessarPedido")
        .WithSummary("Reprocessar pedido pendente")
        .WithDescription(
            "Reenvia um pedido com status **Pendente** para a fila de pagamentos. " +
            "Útil quando o pedido ficou travado por indisponibilidade temporária da PaymentsAPI ou do RabbitMQ.\n\n" +
            "Somente o dono do pedido ou um Administrador pode reprocessar.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – pedido pertence a outro usuário.\n\n" +
            "**404** – pedido não encontrado.\n\n" +
            "**409** – pedido não está no status Pendente (já foi Aprovado ou Rejeitado).")
        .Produces<ReprocessarPedidoResult>(202)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403)
        .Produces<ErroResponse>(404)
        .Produces<ErroResponse>(409);

        return app;
    }

    private record AquisicaoRequest(Guid JogoId);
}
