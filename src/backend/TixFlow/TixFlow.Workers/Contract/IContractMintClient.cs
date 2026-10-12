namespace TixFlow.Workers.Contract;

/// <summary>One confirmed order to mint. Its on-chain nonce is derived from <see cref="OrderId"/>.</summary>
public record MintOrderRequest(Guid OrderId, string Buyer, int TierId, int Quantity);

/// <summary>The buyer-signed EIP-3009 authorisation stored with the order at confirm time.</summary>
public record PaymentAuthorizationRequest(long ValidAfter, long ValidBefore, string Nonce, string Signature);

public interface IContractMintClient
{
    /// <summary>Mints orders that were paid off-chain or are free, in one transaction (MINTER_ROLE path).</summary>
    Task<SentTransaction> MintOrdersAsync(string ticketContract, IReadOnlyList<MintOrderRequest> orders, CancellationToken ct = default);

    /// <summary>Relays a gasless purchase: the buyer pays USDC into escrow and receives tickets atomically.</summary>
    Task<SentTransaction> PurchaseWithAuthorizationAsync(
        string ticketContract, MintOrderRequest order, PaymentAuthorizationRequest payment, CancellationToken ct = default);

    /// <summary>True if the order was already minted, for example self-claimed by the buyer with a voucher.</summary>
    Task<bool> IsOrderMintedAsync(string ticketContract, MintOrderRequest order);
}
