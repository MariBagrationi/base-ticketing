using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

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

        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue("sub");
            var wallet = user.FindFirstValue("wallet");
            return Results.Ok(new { userId, wallet });
        }).RequireAuthorization();
    }
}

public record VerifyRequest(string Message, string Signature);
