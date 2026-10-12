using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace TixFlow.Api.Auth;

public class NonceStore
{
    private static readonly TimeSpan NonceTtl = TimeSpan.FromMinutes(5);
    private const int PruneThreshold = 1024;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _nonces = new();

    public string Generate()
    {
        // Nonces that are requested but never used would otherwise stay in memory forever.
        if (_nonces.Count >= PruneThreshold)
            RemoveExpired();

        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var expiry = DateTimeOffset.UtcNow.Add(NonceTtl);
        _nonces[nonce] = expiry;
        return nonce;
    }

    public bool Validate(string nonce)
    {
        if (!_nonces.TryRemove(nonce, out var expiry))
            return false;

        return DateTimeOffset.UtcNow <= expiry;
    }

    private void RemoveExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _nonces)
        {
            if (now > kvp.Value)
                _nonces.TryRemove(kvp.Key, out _);
        }
    }
}
