using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EDCL.Module.Auth.Infrastructure.Adapters;

/// <summary>
/// JWT token service implementing best-practice Access + Refresh token strategy:
/// - Access Token:  short-lived (15 min), signed HS256, carries claims
/// - Refresh Token: long-lived (30 days), random bytes, stored HASHED in DB
///   (never stored raw — only the hash, like a password)
/// </summary>
public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private readonly IConfiguration _jwtConfig = configuration.GetSection("Jwt");

    public int RefreshTokenExpiryDays =>
        int.Parse(_jwtConfig["RefreshTokenExpiryDays"] ?? "30");

    public (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(Driver driver)
    {
        var secret    = _jwtConfig["AccessTokenSecret"]!;
        var issuer    = _jwtConfig["Issuer"]!;
        var audience  = _jwtConfig["Audience"]!;
        var expiryMin = int.Parse(_jwtConfig["AccessTokenExpiryMinutes"] ?? "15");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMin);

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  driver.Id.ToString()),
            new Claim("driver_id",                  driver.Id.ToString()),
            new Claim("name",                       driver.Name),
            new Claim("nik",                        driver.Nik),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string RawToken, string HashedToken) GenerateRefreshToken()
    {
        // 64 random bytes → Base64 raw token (sent to client)
        var rawBytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert.ToBase64String(rawBytes);

        // SHA-256 hash stored in DB (never store raw refresh tokens!)
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var hashedToken = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return (rawToken, hashedToken);
    }

    public bool TryValidateRefreshToken(string hashedToken, string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var computedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return computedHash == hashedToken;
    }
}
