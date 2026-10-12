using System.Numerics;
using Microsoft.Extensions.Options;
using Nethereum.ABI.EIP712;
using Eip712Domain = Nethereum.ABI.EIP712.Domain;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;

namespace TixFlow.Infrastructure.Chain;

/// <summary>EIP-712 struct signed by the backend; must match MintVoucher in EventTicket.sol field for field.</summary>
[Struct("MintVoucher")]
public class MintVoucherMessage
{
    [Parameter("address", "buyer", 1)]
    public string Buyer { get; set; } = string.Empty;

    [Parameter("uint256", "tierId", 2)]
    public BigInteger TierId { get; set; }

    [Parameter("uint256", "quantity", 3)]
    public BigInteger Quantity { get; set; }

    [Parameter("uint256", "nonce", 4)]
    public BigInteger Nonce { get; set; }

    [Parameter("uint256", "deadline", 5)]
    public BigInteger Deadline { get; set; }
}

public record SignedVoucher(MintVoucherMessage Voucher, string Signature, string TicketContract, long ChainId);

public interface IVoucherSigner
{
    bool IsConfigured { get; }
    string SignerAddress { get; }

    /// <summary>Signs a voucher for one order. Pure computation: no network or chain access.</summary>
    SignedVoucher Sign(string ticketContract, string buyer, int tierId, int quantity, Guid orderId, DateTimeOffset? deadline = null);
}

public class VoucherSigner : IVoucherSigner
{
    public const string DomainName = "TixFlow EventTicket";
    public const string DomainVersion = "1";

    private readonly ChainOptions _options;
    private readonly EthECKey? _key;
    private readonly Eip712TypedDataSigner _signer = new();

    public VoucherSigner(IOptions<ChainOptions> options)
    {
        _options = options.Value;
        if (!string.IsNullOrWhiteSpace(_options.VoucherSignerPrivateKey))
            _key = new EthECKey(_options.VoucherSignerPrivateKey);
    }

    public bool IsConfigured => _key is not null;

    public string SignerAddress => _key?.GetPublicAddress() ?? string.Empty;

    public SignedVoucher Sign(string ticketContract, string buyer, int tierId, int quantity, Guid orderId, DateTimeOffset? deadline = null)
    {
        if (_key is null)
            throw new InvalidOperationException("Chain:VoucherSignerPrivateKey is not configured.");

        var expires = deadline ?? DateTimeOffset.UtcNow.AddMinutes(_options.VoucherLifetimeMinutes);
        var voucher = new MintVoucherMessage
        {
            Buyer = buyer,
            TierId = tierId,
            Quantity = quantity,
            Nonce = ChainIds.OrderNonce(orderId),
            Deadline = expires.ToUnixTimeSeconds()
        };

        var signature = _signer.SignTypedDataV4(voucher, BuildTypedData(ticketContract, _options.ChainId), _key);
        return new SignedVoucher(voucher, signature.EnsureHexPrefix(), ticketContract, _options.ChainId);
    }

    public static TypedData<Eip712Domain> BuildTypedData(string ticketContract, long chainId) => new()
    {
        Domain = new Eip712Domain
        {
            Name = DomainName,
            Version = DomainVersion,
            ChainId = chainId,
            VerifyingContract = ticketContract
        },
        Types = MemberDescriptionFactory.GetTypesMemberDescription(typeof(Eip712Domain), typeof(MintVoucherMessage)),
        PrimaryType = "MintVoucher"
    };
}
