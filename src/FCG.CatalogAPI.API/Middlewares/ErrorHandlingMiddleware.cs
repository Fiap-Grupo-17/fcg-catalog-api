using FCG.CatalogAPI.API.Comum;
using System.Text.Json;

namespace FCG.CatalogAPI.API.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("ArgumentException: {Message}", ex.Message);
            await EscreverErro(ctx, 400, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("KeyNotFoundException: {Message}", ex.Message);
            await EscreverErro(ctx, 404, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("UnauthorizedAccessException: {Message}", ex.Message);
            await EscreverErro(ctx, 403, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("InvalidOperationException: {Message}", ex.Message);
            await EscreverErro(ctx, 409, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado.");
            await EscreverErro(ctx, 500, "Erro interno do servidor.");
        }
    }

    private static async Task EscreverErro(HttpContext ctx, int status, string mensagem)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(
            JsonSerializer.Serialize(new ErroResponse(mensagem)));
    }
}
