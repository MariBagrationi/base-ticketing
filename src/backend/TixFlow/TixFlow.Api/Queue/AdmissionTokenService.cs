using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TixFlow.Api.Queue;

public class AdmissionTokenService
{
    private readonly IConfiguration _config;
    private readonly QueueOptions _options;

    public AdmissionTokenService(IConfiguration config, IOptions<QueueOptions> options)
    {
        _config = config;
        _options = options.Value;
    }

    public (string token, string tokenId) GenerateAdmissionToken(Guid userId, Guid eventId)
    {
        var tokenId = Guid.NewGuid().ToString();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("purpose", "admission"),
            new Claim("eventId", eventId.ToString()),
            new Claim("userId", userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AdmissionTokenTtlMinutes),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), tokenId);
    }

    public AdmissionClaims? ValidateAdmissionToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = key
            }, out _);

            var purpose = principal.FindFirstValue("purpose");
            if (purpose != "admission")
                return null;

            var eventIdStr = principal.FindFirstValue("eventId");
            var userIdStr = principal.FindFirstValue("userId");
            var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);

            if (eventIdStr is null || userIdStr is null || jti is null)
                return null;

            if (!Guid.TryParse(eventIdStr, out var eventId) || !Guid.TryParse(userIdStr, out var userId))
                return null;

            return new AdmissionClaims(userId, eventId, jti);
        }
        catch
        {
            return null;
        }
    }
}

public record AdmissionClaims(Guid UserId, Guid EventId, string TokenId);
