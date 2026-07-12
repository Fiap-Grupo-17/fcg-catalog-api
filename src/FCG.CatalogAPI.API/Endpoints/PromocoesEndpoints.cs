using FCG.CatalogAPI.API.Comum;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Application.Loja.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FCG.CatalogAPI.API.Endpoints;

public static class PromocoesEndpoints
{
    public static IEndpointRouteBuilder MapPromocoesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/promocoes").WithTags("Promoções");

        group.MapGet("/", async (
            bool? ativas,
            bool? vigentes,
            ListarPromocoesHandler handler,
            CancellationToken ct) =>
        {
            var lista = await handler.HandleAsync(ativas, vigentes, ct);
            return Results.Ok(lista);
        })
        .WithName("ListarPromocoes")
        .WithSummary("Listar promoções")
        .WithDescription(
            "Retorna todas as promoções cadastradas. " +
            "Filtros opcionais: `ativas=true` retorna somente promoções ativas; " +
            "`vigentes=true` retorna somente as dentro do período de vigência atual. " +
            "Acesso público, não requer autenticação.")
        .Produces<List<PromocaoResult>>(200);

        group.MapPost("/", async (
            [FromBody] CriarPromocaoCommand cmd,
            CriarPromocaoHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(cmd, ct);
                return Results.Created($"/api/promocoes/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErroResponse(ex.Message));
            }
        })
        .WithName("CriarPromocao")
        .WithSummary("Criar promoção")
        .WithDescription(
            "Cadastra uma nova promoção de desconto vinculada a um ou mais jogos ativos. " +
            "O percentual de desconto deve ser entre 1 e 100. Requer perfil Administrador.\n\n" +
            "**400** – nome vazio, percentual fora do intervalo, data de fim anterior ao início, ou lista de jogos vazia/inexistente.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – usuário autenticado não tem perfil Administrador.")
        .RequireAuthorization("Admin")
        .Produces<PromocaoResult>(201)
        .Produces<ErroResponse>(400)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] AtualizarPromocaoCommand cmd,
            AtualizarPromocaoHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(id, cmd, ct);
                return result is null
                    ? Results.NotFound(new ErroResponse("Promoção não encontrada."))
                    : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErroResponse(ex.Message));
            }
        })
        .WithName("AtualizarPromocao")
        .WithSummary("Atualizar promoção")
        .WithDescription(
            "Atualiza nome, percentual de desconto, período de vigência e lista de jogos de uma promoção existente. " +
            "Todos os jogos informados devem existir e estar ativos. Requer perfil Administrador.\n\n" +
            "**400** – dados de entrada inválidos (ver regras em CriarPromoção).\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – usuário autenticado não tem perfil Administrador.\n\n" +
            "**404** – nenhuma promoção encontrada com o ID informado.")
        .RequireAuthorization("Admin")
        .Produces<PromocaoResult>(200)
        .Produces<ErroResponse>(400)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403)
        .Produces<ErroResponse>(404);

        group.MapPatch("/{id:guid}/encerrar", async (
            Guid id,
            EncerrarPromocaoHandler handler,
            CancellationToken ct) =>
        {
            var ok = await handler.HandleAsync(id, ct);
            return ok
                ? Results.NoContent()
                : Results.NotFound(new ErroResponse("Promoção não encontrada."));
        })
        .WithName("EncerrarPromocao")
        .WithSummary("Encerrar promoção")
        .WithDescription(
            "Desativa permanentemente uma promoção antes do seu prazo de fim. " +
            "Após encerrada, não será mais listada como vigente. Requer perfil Administrador.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – usuário autenticado não tem perfil Administrador.\n\n" +
            "**404** – nenhuma promoção encontrada com o ID informado.")
        .RequireAuthorization("Admin")
        .Produces(204)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403)
        .Produces<ErroResponse>(404);

        return app;
    }
}
