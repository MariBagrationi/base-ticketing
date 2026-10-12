using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using TixFlow.Api.Auth;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Chain;

public static class ChainEndpoints
{
    public static void MapChainEndpoints(this WebApplication app)
    {
        // Everything a browser needs to verify state on-chain by itself, without trusting this API.
        app.MapGet("/chain/config", (IOptions<ChainOptions> options, IVoucherSigner vouchers) =>
        {
            var chain = options.Value;
            if (!chain.Enabled)
                return Results.Ok(new { enabled = false });

            return Results.Ok(new
            {
                enabled = true,
                chainId = chain.ChainId,
                rpcUrl = string.IsNullOrWhiteSpace(chain.PublicRpcUrl) ? chain.RpcUrl : chain.PublicRpcUrl,
                explorerUrl = string.IsNullOrWhiteSpace(chain.ExplorerUrl) ? null : chain.ExplorerUrl!.TrimEnd('/'),
                contracts = new
                {
                    ticketFactory = chain.Contracts.TicketFactory,
                    eventEscrow = chain.Contracts.EventEscrow,
                    priceCappedResale = chain.Contracts.PriceCappedResale,
                    usdc = chain.Contracts.Usdc,
                    signatureValidator = chain.Contracts.SignatureValidator
                },
                voucherSigner = vouchers.SignerAddress,
                faucetEnabled = chain.Faucet.Enabled
            });
        }).WithTags("Chain");

        // A fresh mint voucher for a confirmed order. With it the buyer can mint the tickets on-chain
        // themselves, so a purchase stays redeemable even if the mint worker is down.
        app.MapGet("/orders/{orderId:guid}/voucher", async (
            Guid orderId,
            ClaimsPrincipal user,
            TixFlowDbContext db,
            IVoucherSigner vouchers,
            IOptions<ChainOptions> options) =>
        {
            var userId = Guid.Parse(user.FindFirstValue("sub")!);
            var chain = options.Value;
            if (!chain.Enabled || !vouchers.IsConfigured)
                return Results.Json(new { message = "On-chain claiming is not available in this environment." }, statusCode: 503);

            var order = await db.Orders
                .Include(o => o.Buyer)
                .Include(o => o.TicketTier)
                .Include(o => o.Event)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null)
                return Results.NotFound(new { message = "Order not found." });
            if (order.BuyerId != userId)
                return Results.Json(new { message = "This order belongs to another wallet." }, statusCode: 403);
            if (order.Status != OrderStatus.Confirmed)
                return Results.Conflict(new { message = "Only confirmed orders can be claimed." });
            if (order.Event.ContractAddress is null)
                return Results.Conflict(new { message = "The event's ticket contract is not deployed yet. Try again shortly." });

            var signed = vouchers.Sign(
                order.Event.ContractAddress, order.Buyer.WalletAddress, order.TicketTier.OnChainTierId, order.Quantity, order.Id);

            return Results.Ok(new
            {
                chainId = signed.ChainId,
                ticketContract = signed.TicketContract,
                escrow = chain.Contracts.EventEscrow,
                usdc = chain.Contracts.Usdc,
                voucher = new
                {
                    buyer = signed.Voucher.Buyer,
                    tierId = signed.Voucher.TierId.ToString(),
                    quantity = signed.Voucher.Quantity.ToString(),
                    nonce = signed.Voucher.Nonce.ToString(),
                    deadline = signed.Voucher.Deadline.ToString()
                },
                signature = signed.Signature,
                unitPrice = ChainIds.UsdcUnits(order.TicketTier.PriceUsdc).ToString(),
                payment = order.HasPaymentAuthorization
                    ? new
                    {
                        validAfter = order.PaymentValidAfter.ToString(),
                        validBefore = order.PaymentValidBefore.ToString(),
                        nonce = order.PaymentNonce,
                        signature = order.PaymentSignature
                    }
                    : null
            });
        }).RequireAuthorization(Policies.User).WithTags("Chain");

        // Merkle proof that a ticket was redeemed, against the root currently published on-chain.
        // Anyone holding it can call claimAttendanceBadge; the API is not needed to submit it.
        app.MapGet("/tickets/{ticketId:guid}/attendance-proof", async (
            Guid ticketId,
            ClaimsPrincipal user,
            TixFlowDbContext db) =>
        {
            var userId = Guid.Parse(user.FindFirstValue("sub")!);
            var ticket = await db.Tickets
                .Include(t => t.TicketTier).ThenInclude(tier => tier.Event)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket is null)
                return Results.NotFound(new { message = "Ticket not found." });
            if (ticket.OwnerId != userId && !user.IsAdmin())
                return Results.Json(new { message = "This ticket belongs to another wallet." }, statusCode: 403);
            if (ticket.TokenId is null || ticket.CheckpointedAt is null)
                return Results.Conflict(new { message = "This ticket's redemption has not been checkpointed on-chain yet." });

            var eventId = ticket.TicketTier.EventId;
            var tokenIds = await db.Tickets
                .Where(t => t.TicketTier.EventId == eventId && t.CheckpointedAt != null && t.TokenId != null)
                .Select(t => t.TokenId!.Value)
                .ToListAsync();
            var tree = new MerkleTree(tokenIds);

            return Results.Ok(new
            {
                ticketContract = ticket.TicketTier.Event.ContractAddress,
                tokenId = ticket.TokenId.Value.ToString(),
                root = tree.Root.ToHex(true),
                proof = tree.GetProof(ticket.TokenId.Value).Select(p => p.ToHex(true)),
                alreadyClaimed = ticket.BadgeClaimedAt is not null
            });
        }).RequireAuthorization().WithTags("Chain");

        // Indexed contract logs for an event, newest first. Each entry links to its transaction.
        app.MapGet("/events/{eventId:guid}/activity", async (Guid eventId, TixFlowDbContext db) =>
        {
            var activity = await db.ChainActivities
                .Where(a => a.EventId == eventId)
                .OrderByDescending(a => a.BlockNumber).ThenByDescending(a => a.LogIndex)
                .Take(30)
                .Select(a => new { a.Kind, a.Summary, a.TxHash, a.BlockNumber, a.ContractAddress, a.CreatedAt })
                .ToListAsync();
            return Results.Ok(activity);
        }).WithTags("Chain");
    }
}
