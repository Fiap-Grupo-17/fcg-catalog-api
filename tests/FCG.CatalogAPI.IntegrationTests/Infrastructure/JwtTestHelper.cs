using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FCG.CatalogAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Gera JWTs para testes de integração do CatalogAPI, simulando tokens emitidos pelo UsersAPI.
/// </summary>
public static class JwtTestHelper
{
    private const string Secret   = "CHAVE_DE_TESTE_BEM_LONGA_PARA_HMAC_SHA256_NO_MINIMO_32_CHARS";
    private const string Issuer   = "FCGTests";
    private const string Audience = "FCGTests";

    public static string GerarToken(Guid usuarioId, string role = "Usuario")
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new(JwtRegisteredClaimNames.Email, $"{usuarioId}@test.com"),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GerarTokenAdmin() => GerarToken(Guid.NewGuid(), "Administrador");
    public static string GerarTokenUsuario() => GerarToken(Guid.NewGuid(), "Usuario");
}
