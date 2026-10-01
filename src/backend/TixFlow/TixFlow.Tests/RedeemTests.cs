using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Signer;
using TixFlow.Api.Redeem;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Tests;

public class RedeemTests : IClassFixture<TixFlowWebFactory>
{
    private readonly TixFlowWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public RedeemTests(TixFlowWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullRedemption_ValidSignature_Succeeds()
    {
        var key = EthECKey.GenerateKey();
        var address = key.GetPublicAddress().ToLowerInvariant();
        var (client, ticketId) = await SetupMintedTicket(address);

        var challengeResponse = await client.PostAsync($"/redeem/{ticketId}/challenge", null);
        challengeResponse.EnsureSuccessStatusCode();
        var challengeBody = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>(JsonOpts);

        var signer = new EthereumMessageSigner();
        var signature = signer.EncodeUTF8AndSign(challengeBody!.Challenge, key);

        var confirmResponse = await client.PostAsJsonAsync($"/redeem/{ticketId}/confirm",
            new { challenge = challengeBody.Challenge, signature });
        confirmResponse.EnsureSuccessStatusCode();
        var confirmBody = await confirmResponse.Content.ReadFromJsonAsync<ConfirmResponse>(JsonOpts);

        Assert.True(confirmBody!.Success);
        Assert.Equal(ticketId, confirmBody.TicketId);
        Assert.NotNull(confirmBody.RedeemedAt);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var ticket = await db.Tickets.FindAsync(ticketId);
        Assert.Equal(TicketStatus.Redeemed, ticket!.Status);
        Assert.NotNull(ticket.RedeemedAt);
    }

    [Fact]
    public async Task Redemption_WrongWallet_Rejected()
    {
        var ownerKey = EthECKey.GenerateKey();
        var ownerAddress = ownerKey.GetPublicAddress().ToLowerInvariant();
        var (client, ticketId) = await SetupMintedTicket(ownerAddress);

        var challengeResponse = await client.PostAsync($"/redeem/{ticketId}/challenge", null);
        challengeResponse.EnsureSuccessStatusCode();
        var challengeBody = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>(JsonOpts);

        var wrongKey = EthECKey.GenerateKey();
        var signer = new EthereumMessageSigner();
        var signature = signer.EncodeUTF8AndSign(challengeBody!.Challenge, wrongKey);

        var confirmResponse = await client.PostAsJsonAsync($"/redeem/{ticketId}/confirm",
            new { challenge = challengeBody.Challenge, signature });

        Assert.Equal(HttpStatusCode.Forbidden, confirmResponse.StatusCode);
    }

    [Fact]
    public async Task Redemption_ExpiredChallenge_Rejected()
    {
        var key = EthECKey.GenerateKey();
        var address = key.GetPublicAddress().ToLowerInvariant();
        var (client, ticketId) = await SetupMintedTicket(address);

        var challengeStore = _factory.Services.GetRequiredService<IChallengeStore>() as InMemoryChallengeStore;
        challengeStore!.Clear();

        var signer = new EthereumMessageSigner();
        var signature = signer.EncodeUTF8AndSign("some-fake-challenge", key);

        var confirmResponse = await client.PostAsJsonAsync($"/redeem/{ticketId}/confirm",
            new { challenge = "some-fake-challenge", signature });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, confirmResponse.StatusCode);
        var body = await confirmResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOpts);
        Assert.Contains("expired", body!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Redemption_ReusedChallenge_Rejected()
    {
        var key = EthECKey.GenerateKey();
        var address = key.GetPublicAddress().ToLowerInvariant();
        var (client, ticketId) = await SetupMintedTicket(address);

        var challengeResponse = await client.PostAsync($"/redeem/{ticketId}/challenge", null);
        challengeResponse.EnsureSuccessStatusCode();
        var challengeBody = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>(JsonOpts);

        var signer = new EthereumMessageSigner();
        var signature = signer.EncodeUTF8AndSign(challengeBody!.Challenge, key);

        var first = await client.PostAsJsonAsync($"/redeem/{ticketId}/confirm",
            new { challenge = challengeBody.Challenge, signature });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync($"/redeem/{ticketId}/confirm",
            new { challenge = challengeBody.Challenge, signature });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
    }

    [Fact]
    public async Task Redemption_AlreadyRedeemed_Rejected()
    {
        var key = EthECKey.GenerateKey();
        var address = key.GetPublicAddress().ToLowerInvariant();
        var (client, ticketId) = await SetupMintedTicket(address);

        var challenge1 = await client.PostAsync($"/redeem/{ticketId}/challenge", null);
        var body1 = await challenge1.Content.ReadFromJsonAsync<ChallengeResponse>(JsonOpts);
        var signer = new EthereumMessageSigner();
        var sig1 = signer.EncodeUTF8AndSign(body1!.Challenge, key);
        var confirm1 = await client.PostAsJsonAsync($"/redeem/{ticketId}/confirm",
            new { challenge = body1.Challenge, signature = sig1 });
        confirm1.EnsureSuccessStatusCode();

        var challenge2 = await client.PostAsync($"/redeem/{ticketId}/challenge", null);
        Assert.Equal(HttpStatusCode.Conflict, challenge2.StatusCode);
    }

    [Fact]
    public async Task Challenge_PendingMintTicket_Rejected()
    {
        var (client, ticketId) = await SetupTicketWithStatus(TicketStatus.PendingMint);

        var response = await client.PostAsync($"/redeem/{ticketId}/challenge", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOpts);
        Assert.Contains("not yet minted", body!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Challenge_NonexistentTicket_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/redeem/{Guid.NewGuid()}/challenge", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(HttpClient client, Guid ticketId)> SetupMintedTicket(string walletAddress)
    {
        return await SetupTicketWithStatus(TicketStatus.Minted, walletAddress, tokenId: 42);
    }

    private async Task<(HttpClient client, Guid ticketId)> SetupTicketWithStatus(
        TicketStatus status, string? walletAddress = null, long? tokenId = null)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            WalletAddress = walletAddress ?? "0x" + userId.ToString("N").PadRight(40, '0'),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Redeem Event",
            OrganizerId = userId,
            StartsAt = DateTimeOffset.UtcNow.AddDays(30),
            VenueName = "Test Venue"
        };
        db.Events.Add(evt);

        var tier = new TicketTier
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Name = "GA",
            PriceUsdc = 50m,
            TotalSupply = 100
        };
        db.TicketTiers.Add(tier);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = userId,
            EventId = evt.Id,
            TicketTierId = tier.Id,
            Quantity = 1,
            Status = OrderStatus.Confirmed,
            IdempotencyKey = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Orders.Add(order);

        var ticketId = Guid.NewGuid();
        db.Tickets.Add(new Ticket
        {
            Id = ticketId,
            OrderId = order.Id,
            TicketTierId = tier.Id,
            OwnerId = userId,
            Status = status,
            TokenId = tokenId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        return (client, ticketId);
    }

    private record ChallengeResponse(Guid TicketId, string Challenge, int ExpiresInSeconds);
    private record ConfirmResponse(bool Success, Guid TicketId, DateTimeOffset? RedeemedAt);
    private record ErrorResponse(string Message);
}

public class InMemoryChallengeStore : IChallengeStore
{
    private readonly ConcurrentDictionary<Guid, string> _store = new();

    public Task StoreAsync(Guid ticketId, string challenge, TimeSpan ttl)
    {
        _store[ticketId] = challenge;
        return Task.CompletedTask;
    }

    public Task<string?> ConsumeAsync(Guid ticketId)
    {
        _store.TryRemove(ticketId, out var challenge);
        return Task.FromResult(challenge);
    }

    public void Clear() => _store.Clear();
}
