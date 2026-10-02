using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nethereum.Signer;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Redeem;

public static class RedeemEndpoints
{
    private const int ChallengeTtlSeconds = 60;

    public static void MapRedeemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/redeem").WithTags("Redeem");

        group.MapPost("/{ticketId:guid}/challenge", async (
            Guid ticketId,
            TixFlowDbContext db,
            IChallengeStore challenges) =>
        {
            var ticket = await db.Tickets
                .Where(t => t.Id == ticketId)
                .Select(t => new { t.Id, t.Status })
                .FirstOrDefaultAsync();

            if (ticket is null)
                return Results.NotFound(new { message = "Ticket not found." });

            if (ticket.Status == TicketStatus.Redeemed)
                return Results.Conflict(new { message = "Ticket already redeemed." });

            if (ticket.Status == TicketStatus.PendingMint)
                return Results.UnprocessableEntity(new { message = "Ticket not yet minted." });

            var challenge = GenerateChallenge();
            await challenges.StoreAsync(ticketId, challenge, TimeSpan.FromSeconds(ChallengeTtlSeconds));

            return Results.Ok(new
            {
                ticketId,
                challenge,
                expiresInSeconds = ChallengeTtlSeconds
            });
        });

        group.MapPost("/{ticketId:guid}/confirm", async (
            Guid ticketId,
            [FromBody] ConfirmRedeemRequest request,
            TixFlowDbContext db,
            IChallengeStore challenges) =>
        {
            if (string.IsNullOrWhiteSpace(request.Challenge) || string.IsNullOrWhiteSpace(request.Signature))
                return Results.BadRequest(new { message = "Challenge and signature are required." });

            var storedChallenge = await challenges.ConsumeAsync(ticketId);

            if (storedChallenge is null)
                return Results.UnprocessableEntity(new { message = "Challenge expired or not found." });

            if (storedChallenge != request.Challenge)
                return Results.UnprocessableEntity(new { message = "Challenge mismatch." });

            var ticket = await db.Tickets
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket is null)
                return Results.NotFound(new { message = "Ticket not found." });

            if (ticket.Status == TicketStatus.Redeemed)
                return Results.Conflict(new { message = "Ticket already redeemed." });

            if (ticket.Status == TicketStatus.PendingMint)
                return Results.UnprocessableEntity(new { message = "Ticket not yet minted." });

            string recoveredAddress;
            try
            {
                var signer = new EthereumMessageSigner();
                recoveredAddress = signer.EncodeUTF8AndEcRecover(request.Challenge, request.Signature);
            }
            catch
            {
                return Results.BadRequest(new { message = "Invalid signature." });
            }

            if (!string.Equals(recoveredAddress, ticket.Owner.WalletAddress, StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { message = "Signature doesn't match the ticket owner's wallet." }, statusCode: 403);

            var now = DateTimeOffset.UtcNow;
            ticket.Status = TicketStatus.Redeemed;
            ticket.RedeemedAt = now;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                success = true,
                ticketId = ticket.Id,
                redeemedAt = now
            });
        });
    }

    private static string GenerateChallenge()
    {
        return $"tixflow-redeem-{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
    }
}

public record ConfirmRedeemRequest(string Challenge, string Signature);
