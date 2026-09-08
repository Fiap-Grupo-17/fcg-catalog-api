using FCG.CatalogAPI.API.Comum;
using FCG.CatalogAPI.Application.Loja.CatalogoEstendido;
using FCG.CatalogAPI.Application.Loja.CatalogoEstendido.Commands;
using FCG.CatalogAPI.Application.Loja.CatalogoEstendido.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FCG.CatalogAPI.API.Endpoints;

/// <summary>
/// Endpoints do "catálogo estendido" (read model rico em MongoDB: mídia, screenshots,
/// tags e requisitos de sistema). Arquivo separado de <c>JogosEndpoints.cs</c> — o
/// jogo-base (CRUD principal) continua exclusivamente no PostgreSQL.
/// </summary>
public static class JogosDetalhesEndpoints
{
    public static IEndpointRouteBuilder MapJogosDetalhesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/jogos").WithTags("Catálogo Estendido");

        group.MapPost("/{id:guid}/detalhes", async (
            Guid id,
            [FromBody] UpsertDetalhesJogoCommand cmd,
            UpsertDetalhesJogoHandler handler,
            CancellationToken ct) =>
        {
            var resultado = await handler.HandleAsync(id, cmd, ct);
            return resultado is null
                ? Results.NotFound(new ErroResponse("Jogo não encontrado."))
                : Results.Created($"/api/jogos/{id}/detalhes", resultado);
        })
        .WithName("CriarDetalhesJogo")
        .WithSummary("Cadastrar metadados ricos do jogo (mídia, screenshots, tags, requisitos)")
        .WithDescription(
            "Cria/atualiza (upsert) o read model estendido de um jogo já existente no catálogo. " +
            "Requer perfil Administrador.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – usuário autenticado não tem perfil Administrador.\n\n" +
            "**404** – nenhum jogo-base encontrado com o ID informado.")
        .RequireAuthorization("Admin")
        .Produces<GameExtendedReadModel>(201)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403)
        .Produces<ErroResponse>(404);

        group.MapPut("/{id:guid}/detalhes", async (
            Guid id,
            [FromBody] UpsertDetalhesJogoCommand cmd,
            UpsertDetalhesJogoHandler handler,
            CancellationToken ct) =>
        {
            var resultado = await handler.HandleAsync(id, cmd, ct);
            return resultado is null
                ? Results.NotFound(new ErroResponse("Jogo não encontrado."))
                : Results.Ok(resultado);
        })
        .WithName("AtualizarDetalhesJogo")
        .WithSummary("Atualizar metadados ricos do jogo (mídia, screenshots, tags, requisitos)")
        .WithDescription(
            "Substitui (upsert) o read model estendido de um jogo já existente no catálogo. " +
            "Requer perfil Administrador.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – usuário autenticado não tem perfil Administrador.\n\n" +
            "**404** – nenhum jogo-base encontrado com o ID informado.")
        .RequireAuthorization("Admin")
        .Produces<GameExtendedReadModel>(200)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403)
        .Produces<ErroResponse>(404);

        group.MapGet("/{id:guid}/detalhes", async (
            Guid id,
            BuscarDetalhesJogoHandler handler,
            CancellationToken ct) =>
        {
            var resultado = await handler.HandleAsync(id, ct);
            return resultado is null
                ? Results.NotFound(new ErroResponse("Jogo não encontrado."))
                : Results.Ok(resultado);
        })
        .WithName("BuscarDetalhesJogo")
        .WithSummary("Buscar metadados ricos do jogo (mídia, screenshots, tags, requisitos)")
        .WithDescription(
            "Retorna o read model estendido do jogo. Acesso público, não requer autenticação.\n\n" +
            "Se o jogo ainda não tiver metadados ricos cadastrados, mas existir no catálogo-base, " +
            "um documento esqueleto é criado sob demanda (lazy-seed).\n\n" +
            "**404** – nenhum jogo-base encontrado com o ID informado.")
        .Produces<GameExtendedReadModel>(200)
        .Produces<ErroResponse>(404);

        return app;
    }
}
