using Nethereum.Util;

namespace TixFlow.Infrastructure.Chain;

/// <summary>
/// Merkle tree over redeemed token IDs, verifiable with OpenZeppelin MerkleProof.
/// Leaves are double-hashed <c>keccak256(keccak256(abi.encode(tokenId)))</c> and every parent hashes its
/// two children in sorted order. An unpaired node is carried up unchanged.
/// </summary>
public class MerkleTree
{
    private readonly List<byte[][]> _levels = [];
    private readonly Dictionary<long, int> _indexByToken = [];

    public MerkleTree(IEnumerable<long> tokenIds)
    {
        var ordered = tokenIds.Distinct().Order().ToList();
        if (ordered.Count == 0)
            throw new ArgumentException("A Merkle tree needs at least one leaf.", nameof(tokenIds));

        for (var i = 0; i < ordered.Count; i++)
            _indexByToken[ordered[i]] = i;

        var level = ordered.Select(Leaf).ToArray();
        _levels.Add(level);
        while (level.Length > 1)
        {
            var next = new byte[(level.Length + 1) / 2][];
            for (var i = 0; i < next.Length; i++)
                next[i] = 2 * i + 1 < level.Length ? HashPair(level[2 * i], level[2 * i + 1]) : level[2 * i];
            _levels.Add(next);
            level = next;
        }
    }

    public int Count => _levels[0].Length;

    public byte[] Root => _levels[^1][0];

    public bool Contains(long tokenId) => _indexByToken.ContainsKey(tokenId);

    public List<byte[]> GetProof(long tokenId)
    {
        if (!_indexByToken.TryGetValue(tokenId, out var index))
            throw new KeyNotFoundException($"Token {tokenId} is not in the tree.");

        var proof = new List<byte[]>();
        for (var depth = 0; depth < _levels.Count - 1; depth++)
        {
            var level = _levels[depth];
            var sibling = index ^ 1;
            if (sibling < level.Length)
                proof.Add(level[sibling]);
            index /= 2;
        }
        return proof;
    }

    public static bool Verify(IEnumerable<byte[]> proof, byte[] root, long tokenId)
    {
        var computed = proof.Aggregate(Leaf(tokenId), HashPair);
        return computed.AsSpan().SequenceEqual(root);
    }

    public static byte[] Leaf(long tokenId)
    {
        var hasher = Sha3Keccack.Current;
        return hasher.CalculateHash(hasher.CalculateHash(ChainIds.ToBytes32(tokenId)));
    }

    private static byte[] HashPair(byte[] a, byte[] b)
    {
        var (first, second) = a.AsSpan().SequenceCompareTo(b) <= 0 ? (a, b) : (b, a);
        var combined = new byte[64];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, 32);
        return Sha3Keccack.Current.CalculateHash(combined);
    }
}
