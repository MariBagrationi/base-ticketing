using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nethereum.Signer;
using TixFlow.Domain.Entities;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Auth;

public class SiweService
{
    private readonly NonceStore _nonceStore;
    private readonly TixFlowDbContext _db;
    private readonly IConfiguration _config;

    public SiweService(NonceStore nonceStore, TixFlowDbContext db, IConfiguration config)
    {
        _nonceStore = nonceStore;
        _db = db;
        _config = config;
    }

    public string GenerateNonce() => _nonceStore.Generate();

    public async Task<string?> VerifyAndAuthenticate(string message, string signature)
    {
        var parsed = SiweMessage.Parse(message);
        if (parsed is null)
            return null;

        if (!_nonceStore.Validate(parsed.Nonce))
            return null;

        string recoveredAddress;
        try
        {
            var signer = new EthereumMessageSigner();
            recoveredAddress = signer.EncodeUTF8AndEcRecover(message, signature);
        }
        catch
        {
            return null;
        }

        if (!string.Equals(recoveredAddress, parsed.Address, StringComparison.OrdinalIgnoreCase))
            return null;

        var normalizedAddress = parsed.Address.ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.WalletAddress == normalizedAddress);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                WalletAddress = normalizedAddress,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        return GenerateJwt(user);
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryHours = int.Parse(_config["Jwt:ExpiryHours"] ?? "24");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("wallet", user.WalletAddress),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
