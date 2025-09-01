using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RdpRelay.Portal.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RdpRelay.Portal.Api.Services;

public interface IJwtService
{
    string GenerateAccessToken(string tenantId, string userId, string role);
    string GenerateSessionToken(string sessionId, string agentId, string tenantId, string operatorUserId);
    string GenerateProvisioningToken(string agentId, string tenantId, string? groupId = null);
    ClaimsPrincipal? ValidateToken(string token);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
}

public class JwtService : IJwtService
{
    private readonly JwtOptions _jwtOptions;
    private readonly SecurityKey _signingKey;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string GenerateAccessToken(string tenantId, string userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
            new("tenant_id", tenantId),
            new("tokenType", "access")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(_jwtOptions.AccessTokenExpiry),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public string GenerateSessionToken(string sessionId, string agentId, string tenantId, string operatorUserId)
    {
        var claims = new List<Claim>
        {
            new("sid", sessionId),
            new("aid", agentId),
            new("tid", tenantId),
            new("op", operatorUserId),
            new("tokenType", "session")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(_jwtOptions.SessionTokenExpiry),
            Issuer = _jwtOptions.Issuer,
            Audience = "relay",
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public string GenerateProvisioningToken(string agentId, string tenantId, string? groupId = null)
    {
        var claims = new List<Claim>
        {
            new("sub", agentId),
            new("tid", tenantId),
            new("tokenType", "provisioning")
        };

        if (!string.IsNullOrEmpty(groupId))
        {
            claims.Add(new Claim("gid", groupId));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(365), // Long-lived for agent provisioning
            Issuer = _jwtOptions.Issuer,
            Audience = "relay",
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudiences = new[] { _jwtOptions.Audience, "relay" },
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        // In a production system, you would:
        // 1. Look up the refresh token in your database/cache
        // 2. Verify it hasn't expired
        // 3. Return the associated claims
        
        // For this simplified implementation, we'll decode a JWT refresh token
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidAudience = _jwtOptions.Audience,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(refreshToken, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
