using Microsoft.Extensions.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Contracts.EventTicket.ContractDefinition;
using Escrow = TixFlow.Infrastructure.Contracts.EventEscrow.ContractDefinition;

namespace TixFlow.Workers.Contract;

/// <summary>Typed client for the two mint paths, built on the generated contract bindings.</summary>
public class ContractMintClient : IContractMintClient
{
    private readonly IRelayer _relayer;
    private readonly IVoucherSigner _vouchers;
    private readonly ChainOptions _options;

    public ContractMintClient(IRelayer relayer, IVoucherSigner vouchers, IOptions<ChainOptions> options)
    {
        _relayer = relayer;
        _vouchers = vouchers;
        _options = options.Value;
    }

    public Task<SentTransaction> MintOrdersAsync(
        string ticketContract, IReadOnlyList<MintOrderRequest> orders, CancellationToken ct = default)
    {
        var function = new MintBatchFunction
        {
            Orders = orders.Select(o => new MintOrder
            {
                Buyer = o.Buyer,
                TierId = o.TierId,
                Quantity = o.Quantity,
                Nonce = ChainIds.OrderNonce(o.OrderId)
            }).ToList()
        };
        return _relayer.SendAsync(ticketContract, function, ct);
    }

    public Task<SentTransaction> PurchaseWithAuthorizationAsync(
        string ticketContract, MintOrderRequest order, PaymentAuthorizationRequest payment, CancellationToken ct = default)
    {
        // The voucher is signed just before submission, so it cannot expire while the order waits for the chain.
        var signed = _vouchers.Sign(ticketContract, order.Buyer, order.TierId, order.Quantity, order.OrderId);
        var paymentSignature = EthECDSASignatureFactory.ExtractECDSASignature(payment.Signature);

        var function = new Escrow.PurchaseWithAuthorizationFunction
        {
            Ticket = ticketContract,
            Voucher = new Escrow.MintVoucher
            {
                Buyer = signed.Voucher.Buyer,
                TierId = signed.Voucher.TierId,
                Quantity = signed.Voucher.Quantity,
                Nonce = signed.Voucher.Nonce,
                Deadline = signed.Voucher.Deadline
            },
            VoucherSignature = signed.Signature.HexToByteArray(),
            Auth = new Escrow.PaymentAuthorization
            {
                ValidAfter = payment.ValidAfter,
                ValidBefore = payment.ValidBefore,
                Nonce = payment.Nonce.HexToByteArray(),
                V = paymentSignature.V[0],
                R = paymentSignature.R,
                S = paymentSignature.S
            }
        };
        return _relayer.SendAsync(_options.Contracts.EventEscrow, function, ct);
    }

    public Task<bool> IsOrderMintedAsync(string ticketContract, MintOrderRequest order) =>
        _relayer.QueryAsync<NonceUsedFunction, bool>(ticketContract, new NonceUsedFunction
        {
            Buyer = order.Buyer,
            Nonce = ChainIds.OrderNonce(order.OrderId)
        });
}
