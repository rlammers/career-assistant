using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace CareerAssistant.Api.Tests;

public static class TestJwtTokens
{
    public const string TenantId = "11111111-1111-1111-1111-111111111111";
    public const string Issuer = $"https://login.microsoftonline.com/{TenantId}/v2.0";
    public const string Audience = "22222222-2222-2222-2222-222222222222";
    public const string AppRole = "CareerAssistant.Demo.Access";

    public static readonly SymmetricSecurityKey SigningKey = new("career-assistant-test-signing-key-32"u8.ToArray());

    public static string Create(
        string? issuer = null,
        string? audience = null,
        string? tenantId = null,
        DateTime? expires = null)
    {
        var claims = new[]
        {
            new Claim("tid", tenantId ?? TenantId),
            new Claim("roles", AppRole)
        };
        var credentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);
        var expiry = expires ?? DateTime.UtcNow.AddMinutes(5);
        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer,
            audience: audience ?? Audience,
            claims: claims,
            notBefore: expiry.AddMinutes(-1),
            expires: expiry,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
