using System.Numerics;
using Microsoft.Extensions.Options;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using TixFlow.Infrastructure.Chain;

namespace TixFlow.Tests;

/// <summary>
/// Pure-computation tests for the pieces that must agree byte-for-byte with the Solidity contracts.
/// Each expected value is derived here independently, the way the contract derives it.
/// </summary>
public class ChainPrimitivesTests
{
    // A well-known public Anvil development key. Never use it outside local tests.
    private const string SignerKey = "0x5de4111afa1a4b94908f83103eb1f1706367c2e68ca870fc3fb9a804cdab365a";
    private const string SignerAddress = "0x3C44CdDdB6a900fa2b585dd299e03d12FA4293BC";
    private const string TicketContract = "0x5FbDB2315678afecb367f032d93F642f64180aa3";
    private const string Buyer = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8";
    private const long ChainId = 31337;

    private static readonly Sha3Keccack Keccak = Sha3Keccack.Current;

    // ----- order and event identifiers -------------------------------------

    [Fact]
    public void OrderNonce_RoundTrips()
    {
        var orderId = Guid.Parse("a1e0c001-5eed-4000-8000-0000000000ff");
        var nonce = ChainIds.OrderNonce(orderId);

        Assert.Equal(BigInteger.Parse("0a1e0c0015eed400080000000000000ff", System.Globalization.NumberStyles.HexNumber), nonce);
        Assert.Equal(orderId, ChainIds.OrderIdFromNonce(nonce));
    }

    [Fact]
    public void OrderNonce_RoundTrips_ForRandomGuids()
    {
        for (var i = 0; i < 200; i++)
        {
            var orderId = Guid.NewGuid();
            Assert.Equal(orderId, ChainIds.OrderIdFromNonce(ChainIds.OrderNonce(orderId)));
        }
    }

    [Fact]
    public void OrderIdFromNonce_RejectsValuesThatAreNotGuids()
    {
        Assert.Null(ChainIds.OrderIdFromNonce(BigInteger.Pow(2, 128)));
        Assert.Null(ChainIds.OrderIdFromNonce(BigInteger.MinusOne));
    }

    [Fact]
    public void EventId_IsLeftPaddedGuid_AndRoundTrips()
    {
        var eventId = Guid.Parse("a1e0c001-5eed-4000-8000-000000000001");
        var bytes = ChainIds.EventId(eventId);

        Assert.Equal("0x00000000000000000000000000000000a1e0c0015eed40008000000000000001", bytes.ToHex(true));
        Assert.Equal(eventId, ChainIds.EventIdFromBytes(bytes));
        Assert.Null(ChainIds.EventIdFromBytes(new byte[31]));

        bytes[0] = 1;
        Assert.Null(ChainIds.EventIdFromBytes(bytes));
    }

    [Theory]
    [InlineData("25", "25000000")]
    [InlineData("0.5", "500000")]
    [InlineData("0", "0")]
    [InlineData("1234.567891", "1234567891")]
    public void UsdcUnits_UsesSixDecimals(string amount, string expected)
    {
        Assert.Equal(BigInteger.Parse(expected), ChainIds.UsdcUnits(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    // ----- EIP-712 vouchers ------------------------------------------------

    [Fact]
    public void VoucherSignature_MatchesTheDigestTheContractComputes()
    {
        var signer = CreateSigner();
        var orderId = Guid.NewGuid();
        var deadline = DateTimeOffset.FromUnixTimeSeconds(1_900_000_000);

        var signed = signer.Sign(TicketContract, Buyer, tierId: 2, quantity: 3, orderId, deadline);

        // Rebuild EventTicket.hashVoucher by hand: keccak256("\x19\x01" ‖ domainSeparator ‖ structHash).
        var abi = new ABIEncode();
        var domainSeparator = Keccak.CalculateHash(abi.GetABIEncoded(
            new ABIValue("bytes32", Keccak.CalculateHash(
                "EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)"u8.ToArray())),
            new ABIValue("bytes32", Keccak.CalculateHash("TixFlow EventTicket"u8.ToArray())),
            new ABIValue("bytes32", Keccak.CalculateHash("1"u8.ToArray())),
            new ABIValue("uint256", new BigInteger(ChainId)),
            new ABIValue("address", TicketContract)));
        var structHash = Keccak.CalculateHash(abi.GetABIEncoded(
            new ABIValue("bytes32", Keccak.CalculateHash(
                "MintVoucher(address buyer,uint256 tierId,uint256 quantity,uint256 nonce,uint256 deadline)"u8.ToArray())),
            new ABIValue("address", Buyer),
            new ABIValue("uint256", new BigInteger(2)),
            new ABIValue("uint256", new BigInteger(3)),
            new ABIValue("uint256", ChainIds.OrderNonce(orderId)),
            new ABIValue("uint256", new BigInteger(1_900_000_000))));
        var digest = Keccak.CalculateHash([0x19, 0x01, .. domainSeparator, .. structHash]);

        var recovered = new MessageSigner().EcRecover(digest, signed.Signature);

        Assert.Equal(SignerAddress, recovered, ignoreCase: true);
        Assert.Equal(SignerAddress, signer.SignerAddress, ignoreCase: true);
        Assert.Equal(ChainId, signed.ChainId);
        Assert.Equal(ChainIds.OrderNonce(orderId), signed.Voucher.Nonce);
    }

    [Fact]
    public void VoucherSignature_IsBoundToContractAndChain()
    {
        var orderId = Guid.NewGuid();
        var deadline = DateTimeOffset.FromUnixTimeSeconds(1_900_000_000);

        var a = CreateSigner().Sign(TicketContract, Buyer, 1, 1, orderId, deadline);
        var otherContract = CreateSigner().Sign("0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512", Buyer, 1, 1, orderId, deadline);
        var otherChain = CreateSigner(chainId: 84532).Sign(TicketContract, Buyer, 1, 1, orderId, deadline);

        Assert.NotEqual(a.Signature, otherContract.Signature);
        Assert.NotEqual(a.Signature, otherChain.Signature);
    }

    [Fact]
    public void VoucherSigner_DefaultDeadline_UsesConfiguredLifetime()
    {
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signed = CreateSigner(lifetimeMinutes: 30).Sign(TicketContract, Buyer, 1, 1, Guid.NewGuid());
        var deadline = (long)signed.Voucher.Deadline;

        Assert.InRange(deadline, before + 30 * 60 - 5, before + 30 * 60 + 5);
    }

    [Fact]
    public void VoucherSigner_WithoutKey_IsNotConfiguredAndRefusesToSign()
    {
        var signer = new VoucherSigner(Options.Create(new ChainOptions()));

        Assert.False(signer.IsConfigured);
        Assert.Equal(string.Empty, signer.SignerAddress);
        Assert.Throws<InvalidOperationException>(() => signer.Sign(TicketContract, Buyer, 1, 1, Guid.NewGuid()));
    }

    // ----- Merkle tree (OpenZeppelin-compatible) ---------------------------

    [Fact]
    public void MerkleLeaf_IsDoubleKeccakOfAbiEncodedTokenId()
    {
        var encoded = new byte[32];
        encoded[31] = 7;
        var expected = Keccak.CalculateHash(Keccak.CalculateHash(encoded));

        Assert.Equal(expected, MerkleTree.Leaf(7));
    }

    [Fact]
    public void MerkleTree_SingleLeaf_RootIsTheLeafAndProofIsEmpty()
    {
        var tree = new MerkleTree([42]);

        Assert.Equal(MerkleTree.Leaf(42), tree.Root);
        Assert.Empty(tree.GetProof(42));
        Assert.True(MerkleTree.Verify([], tree.Root, 42));
    }

    [Fact]
    public void MerkleTree_TwoLeaves_RootIsHashOfSortedPair()
    {
        var a = MerkleTree.Leaf(1);
        var b = MerkleTree.Leaf(2);
        var (first, second) = a.AsSpan().SequenceCompareTo(b) <= 0 ? (a, b) : (b, a);
        var expected = Keccak.CalculateHash([.. first, .. second]);

        Assert.Equal(expected, new MerkleTree([2, 1]).Root);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(13)]
    [InlineData(100)]
    public void MerkleTree_EveryLeafHasAValidProof(int size)
    {
        var tokenIds = Enumerable.Range(1, size).Select(i => (long)i * 3).ToList();
        var tree = new MerkleTree(tokenIds);

        Assert.Equal(size, tree.Count);
        foreach (var tokenId in tokenIds)
        {
            Assert.True(tree.Contains(tokenId));
            Assert.True(MerkleTree.Verify(tree.GetProof(tokenId), tree.Root, tokenId));
        }
    }

    [Fact]
    public void MerkleTree_RejectsTokensThatAreNotInIt()
    {
        var tree = new MerkleTree([1, 2, 3, 4]);

        Assert.False(tree.Contains(5));
        Assert.Throws<KeyNotFoundException>(() => tree.GetProof(5));
        Assert.False(MerkleTree.Verify(tree.GetProof(1), tree.Root, 5));
    }

    [Fact]
    public void MerkleTree_IsOrderIndependentAndIgnoresDuplicates()
    {
        Assert.Equal(new MerkleTree([3, 1, 2]).Root, new MerkleTree([1, 2, 2, 3, 3]).Root);
        Assert.Throws<ArgumentException>(() => new MerkleTree([]));
    }

    // ----- RPC failure classification --------------------------------------

    [Fact]
    public void TransportFailures_AreDistinguishedFromNodeErrors()
    {
        Assert.True(ChainRpc.IsTransportFailure(new HttpRequestException("connection refused")));
        Assert.True(ChainRpc.IsTransportFailure(new TaskCanceledException()));
        Assert.True(ChainRpc.IsTransportFailure(new Exception("wrapped", new TimeoutException())));
        Assert.False(ChainRpc.IsTransportFailure(new InvalidOperationException("execution reverted")));
        Assert.False(ChainRpc.IsTransportFailure(
            new Nethereum.JsonRpc.Client.RpcResponseException(new Nethereum.JsonRpc.Client.RpcError(3, "execution reverted"))));
    }

    private static VoucherSigner CreateSigner(long chainId = ChainId, int lifetimeMinutes = 60) =>
        new(Options.Create(new ChainOptions
        {
            ChainId = chainId,
            VoucherSignerPrivateKey = SignerKey,
            VoucherLifetimeMinutes = lifetimeMinutes
        }));
}
