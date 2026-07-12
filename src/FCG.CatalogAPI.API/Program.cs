using FCG.CatalogAPI.Application.Biblioteca.Commands;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Application.Loja.Queries;
using FCG.CatalogAPI.API.Endpoints;
using FCG.CatalogAPI.API.Middlewares;
using FCG.CatalogAPI.Infrastructure.Mensageria;
using FCG.CatalogAPI.Infrastructure.Persistencia;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

// Em Testing, NÃO inicializa o Serilog bootstrap logger nem o UseSerilog.
// Motivo: xUnit executa classes de teste em paralelo; cada classe cria sua própria
// WebApplicationFactory, que chama Program.cs novamente. O ReloadableLogger global
// (Log.Logger) só pode ser "frozen" uma vez por processo — a segunda tentativa lança
// InvalidOperationException: "The logger is already frozen."
var isTestEnv = string.Equals(
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    "Testing",
    StringComparison.OrdinalIgnoreCase);

if (!isTestEnv)
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();
}

var builder = WebApplication.CreateBuilder(args);

if (!isTestEnv)
{
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());
}

// ── EF Core — usa InMemory se o ambiente for "Testing" OU se UseInMemoryDatabase=true no config
// (duplo check: garante InMemory mesmo se appsettings.Testing.json não for carregado a tempo)
var useInMemory = builder.Environment.IsEnvironment("Testing")
               || builder.Configuration.GetValue<bool>("UseInMemoryDatabase");

if (useInMemory)
    builder.Services.AddDbContext<CatalogDbContext>(opt =>
        opt.UseInMemoryDatabase("fcg-catalog-tests"));
else
    builder.Services.AddDbContext<CatalogDbContext>(opt =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<ICatalogDbContext>(p => p.GetRequiredService<CatalogDbContext>());

// ── Application Handlers ───────────────────────────────────────────
builder.Services.AddScoped<CriarJogoHandler>();
builder.Services.AddScoped<AtualizarJogoHandler>();
builder.Services.AddScoped<DesativarJogoHandler>();
builder.Services.AddScoped<AtivarJogoHandler>();
builder.Services.AddScoped<IniciarAquisicaoHandler>();
builder.Services.AddScoped<ListarJogosHandler>();
builder.Services.AddScoped<BuscarJogoHandler>();
builder.Services.AddScoped<BuscarPedidoHandler>();
builder.Services.AddScoped<ListarPedidosHandler>();
builder.Services.AddScoped<ReprocessarPedidoHandler>();
builder.Services.AddScoped<RegistrarItemBibliotecaHandler>();
builder.Services.AddScoped<CriarPromocaoHandler>();
builder.Services.AddScoped<AtualizarPromocaoHandler>();
builder.Services.AddScoped<EncerrarPromocaoHandler>();
builder.Services.AddScoped<ListarPromocoesHandler>();

// ── MassTransit + RabbitMQ ─────────────────────────────────────────
builder.Services.AddScoped<IEventBus, MassTransitEventBus>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentProcessedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMQ:Host"] ?? "localhost",
            builder.Configuration["RabbitMQ:VirtualHost"] ?? "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });

        cfg.ConfigureEndpoints(ctx);
    });
});

// ── JWT (valida tokens emitidos pelo UsersAPI) ─────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret não configurado.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("Admin", p => p.RequireRole("Administrador"));
});

// ── Swagger ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FCG Catalog API",
        Version = "v1",
        Description =
            "API de catálogo da plataforma FCG. Gerencia jogos, promoções e biblioteca pessoal dos usuários.\n\n" +
            "### Autenticação\n" +
            "Obtenha um token JWT em `POST /api/auth/login` na **Users API** e inclua-o no cabeçalho `Authorization: Bearer {token}`.\n\n" +
            "### Resposta de erro padrão\n" +
            "Todos os erros retornam o schema `ErroResponse` com o campo `erro` descrevendo o problema."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe apenas o token JWT obtido na Users API (`POST /api/auth/login`), sem o prefixo \"Bearer\". O Swagger adiciona o prefixo automaticamente."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    // Garante que ErroResponse apareça no schema global
    c.UseAllOfToExtendReferenceSchemas();
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

// Swagger disponível em todos os ambientes exceto Production
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FCG Catalog API v1");
        c.DocumentTitle = "FCG Catalog API";
        c.DefaultModelsExpandDepth(2);   // expande schemas de erro por padrão
        c.DisplayRequestDuration();
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "FCG.CatalogAPI",
    timestamp = DateTime.UtcNow
}));

app.MapJogosEndpoints();
app.MapBibliotecaEndpoints();
app.MapPromocoesEndpoints();

// ── Migrations automáticas — só executa quando o banco é relacional (Postgres).
// InMemory retorna IsRelational() == false, portanto é ignorado aqui.
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    if (db.Database.IsRelational())
    {
        try
        {
            // EnsureCreated cria o schema a partir do modelo EF (sem precisar de migrations).
            // Ao gerar migrations formais no futuro, substituir por MigrateAsync().
            await db.Database.EnsureCreatedAsync();
            if (!isTestEnv) Log.Information("✅ Schema criado/verificado (loja + biblioteca)");
        }
        catch (Exception ex)
        {
            if (!isTestEnv) Log.Fatal(ex, "❌ Falha ao criar schema. Postgres está rodando? (Docker deve estar ativo)");
            Console.Error.WriteLine($"FATAL: Falha ao criar schema: {ex.Message}");
            throw;
        }
    }
}

if (!isTestEnv) Log.Information("🚀 FCG.CatalogAPI iniciando...");
app.Run();

public partial class Program { }
