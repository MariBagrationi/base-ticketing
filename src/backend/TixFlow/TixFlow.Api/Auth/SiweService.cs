using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using TixFlow.Domain.Entities;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Auth;

public class SiweService
{
    private readonly NonceStore _nonceStore;
    private readonly TixFlowDbContext _db;
    private readonly IConfiguration _config;
    private readonly ISmartWalletSignatureVerifier _smartWallets;
    private readonly RoleService _roles;

    public SiweService(
        NonceStore nonceStore,
        TixFlowDbContext db,
        IConfiguration config,
        ISmartWalletSignatureVerifier smartWallets,
        RoleService roles)
    {
        _nonceStore = nonceStore;
        _db = db;
        _config = config;
        _smartWallets = smartWallets;
        _roles = roles;
    }

    public string GenerateNonce() => _nonceStore.Generate();

    public async Task<string?> VerifyAndAuthenticate(string message, string signature)
    {
        var parsed = SiweMessage.Parse(message);
        if (parsed is null)
            return null;

        if (!_nonceStore.Validate(parsed.Nonce))
            return null;

        if (!await IsValidSignatureAsync(message, signature, parsed.Address))
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
        }

        // A wallet gets the default User role the first time it signs in, whether its row was just created
        // or already existed (pre-provisioned staff, or a resale buyer the indexer recorded).
        var firstLogin = user.FirstLoginAt is null;
        user.FirstLoginAt ??= DateTimeOffset.UtcNow;
        var hasRoles = await _db.UserRoles.AnyAsync(r => r.UserId == user.Id);
        var roles = await _roles.EnsureLoginRolesAsync(user, isNewUser: firstLogin && !hasRoles);

        return GenerateJwt(user, roles);
    }

    /// <summary>
    /// Accepts an EOA signature (checked offline with ecrecover) or, when a chain is configured, a smart-wallet
    /// signature: ERC-1271 for deployed wallets and ERC-6492 for counterfactual ones such as Coinbase Smart Wallet.
    /// </summary>
    private async Task<bool> IsValidSignatureAsync(string message, string signature, string claimedAddress)
    {
        var signer = new EthereumMessageSigner();
        try
        {
            var recovered = signer.EncodeUTF8AndEcRecover(message, signature);
            if (string.Equals(recovered, claimedAddress, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        catch
        {
            // Not a 65-byte ECDSA signature; it may still be a contract-wallet signature.
        }

        if (!_smartWallets.IsAvailable)
            return false;

        byte[] signatureBytes;
        try
        {
            signatureBytes = signature.HexToByteArray();
        }
        catch
        {
            return false;
        }

        var hash = signer.HashPrefixedMessage(Encoding.UTF8.GetBytes(message));
        return await _smartWallets.IsValidAsync(claimedAddress, hash, signatureBytes);
    }

    private string GenerateJwt(User user, IEnumerable<Domain.Enums.Role> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryHours = int.Parse(_config["Jwt:ExpiryHours"] ?? "24");

        // Role claims describe the roles at sign-in for clients. The API itself re-reads roles from the
        // database on every request, so a revoked role stops working immediately.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("wallet", user.WalletAddress),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(Policies.RoleClaim, r.ToString())));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
