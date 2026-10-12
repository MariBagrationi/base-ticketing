using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapGet("/nonce", (SiweService siwe) =>
        {
            var nonce = siwe.GenerateNonce();
            return Results.Ok(new { nonce });
        });

        group.MapPost("/verify", async (SiweService siwe, [FromBody] VerifyRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message) || string.IsNullOrWhiteSpace(request.Signature))
                return Results.BadRequest(new { error = "Message and signature are required." });

            var token = await siwe.VerifyAndAuthenticate(request.Message, request.Signature);
            if (token is null)
                return Results.Unauthorized();

            return Results.Ok(new { token });
        });

        // Roles here are the live ones from the database, not the snapshot inside the token.
        group.MapGet("/me", async (ClaimsPrincipal user, TixFlowDbContext db) =>
        {
            var userId = user.UserId();
            var staffEventIds = await db.StaffAssignments
                .Where(a => a.UserId == userId)
                .Select(a => a.EventId)
                .ToListAsync();

            return Results.Ok(new
            {
                userId,
                wallet = user.Wallet(),
                roles = user.Roles(),
                staffEventIds
            });
        }).RequireAuthorization();
    }
}

public record VerifyRequest(string Message, string Signature);
