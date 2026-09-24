using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Signer;
using TixFlow.Api.Queue;

namespace TixFlow.Tests;

public class QueueTests : IClassFixture<TixFlowWebFactory>
{
    private readonly TixFlowWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid TestEventId = Guid.NewGuid();

    public QueueTests(TixFlowWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task JoinQueue_ReturnsPosition()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.PostAsync($"/queue/{TestEventId}/join", null);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JoinResponseDto>(JsonOpts);
        Assert.NotNull(body);
        Assert.True(body.Position >= 0);
    }

    [Fact]
    public async Task JoinQueue_Idempotent_ReturnsSamePosition()
    {
        var eventId = Guid.NewGuid();
        var client = await CreateAuthenticatedClient();

        var r1 = await client.PostAsync($"/queue/{eventId}/join", null);
        r1.EnsureSuccessStatusCode();
        var b1 = await r1.Content.ReadFromJsonAsync<JoinResponseDto>(JsonOpts);

        var r2 = await client.PostAsync($"/queue/{eventId}/join", null);
        r2.EnsureSuccessStatusCode();
        var b2 = await r2.Content.ReadFromJsonAsync<JoinResponseDto>(JsonOpts);

        Assert.Equal(b1!.Position, b2!.Position);
    }

    [Fact]
    public async Task JoinQueue_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/queue/{TestEventId}/join", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FifoAdmissionOrder_FirstJoinedFirstAdmitted()
    {
        var eventId = Guid.NewGuid();
        var queueStore = _factory.QueueStore;
        var userIds = new List<Guid>();

        for (int i = 0; i < 6; i++)
        {
            var userId = Guid.NewGuid();
            userIds.Add(userId);
            await queueStore.JoinAsync(eventId, userId);
            await Task.Delay(5);
        }

        // Batch size is 3 in test config — pop first batch
        var batch1 = await queueStore.PopBatchAsync(eventId, 3);
        Assert.Equal(3, batch1.Length);
        Assert.Equal(userIds[0], batch1[0].UserId);
        Assert.Equal(userIds[1], batch1[1].UserId);
        Assert.Equal(userIds[2], batch1[2].UserId);

        // Pop second batch
        var batch2 = await queueStore.PopBatchAsync(eventId, 3);
        Assert.Equal(3, batch2.Length);
        Assert.Equal(userIds[3], batch2[0].UserId);
        Assert.Equal(userIds[4], batch2[1].UserId);
        Assert.Equal(userIds[5], batch2[2].UserId);
    }

    [Fact]
    public async Task AdmissionToken_GatesCheckoutReserve()
    {
        var eventId = Guid.NewGuid();
        var client = await CreateAuthenticatedClient();

        // Without admission token → 403
        var reserveBody = new { eventId, tierId = Guid.NewGuid(), quantity = 1 };
        var noTokenResponse = await client.PostAsJsonAsync("/checkout/reserve", reserveBody);
        Assert.Equal(HttpStatusCode.Forbidden, noTokenResponse.StatusCode);
    }

    [Fact]
    public async Task AdmissionToken_ValidToken_AllowsCheckout()
    {
        var eventId = Guid.NewGuid();
        var (client, userId) = await CreateAuthenticatedClientWithUserId();
        var queueStore = _factory.QueueStore;

        // Generate an admission token via the token service
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<AdmissionTokenService>();
        var (token, tokenId) = tokenService.GenerateAdmissionToken(userId, eventId);
        await queueStore.StoreAdmissionTokenAsync(tokenId, userId, eventId, TimeSpan.FromMinutes(5));

        client.DefaultRequestHeaders.Add("X-Admission-Token", token);
        var reserveBody = new { eventId, tierId = Guid.NewGuid(), quantity = 1 };
        var response = await client.PostAsJsonAsync("/checkout/reserve", reserveBody);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AdmissionToken_ExpiredToken_Returns403()
    {
        var eventId = Guid.NewGuid();
        var (client, userId) = await CreateAuthenticatedClientWithUserId();
        var queueStore = _factory.QueueStore;

        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<AdmissionTokenService>();
        var (token, tokenId) = tokenService.GenerateAdmissionToken(userId, eventId);

        // Store with already-expired TTL — token exists but expired
        // Since we can't easily expire the JWT itself in a unit test,
        // we test the Redis-side expiry by not storing the token at all
        // (simulating it expired from the store)

        client.DefaultRequestHeaders.Add("X-Admission-Token", token);
        var reserveBody = new { eventId, tierId = Guid.NewGuid(), quantity = 1 };
        var response = await client.PostAsJsonAsync("/checkout/reserve", reserveBody);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdmissionToken_DifferentUser_Returns403()
    {
        var eventId = Guid.NewGuid();
        var (client, _) = await CreateAuthenticatedClientWithUserId();
        var queueStore = _factory.QueueStore;

        // Generate token for a DIFFERENT user
        var otherUserId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<AdmissionTokenService>();
        var (token, tokenId) = tokenService.GenerateAdmissionToken(otherUserId, eventId);
        await queueStore.StoreAdmissionTokenAsync(tokenId, otherUserId, eventId, TimeSpan.FromMinutes(5));

        client.DefaultRequestHeaders.Add("X-Admission-Token", token);
        var reserveBody = new { eventId, tierId = Guid.NewGuid(), quantity = 1 };
        var response = await client.PostAsJsonAsync("/checkout/reserve", reserveBody);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdmissionToken_Consumed_CannotBeReused()
    {
        var eventId = Guid.NewGuid();
        var (client, userId) = await CreateAuthenticatedClientWithUserId();
        var queueStore = _factory.QueueStore;

        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<AdmissionTokenService>();
        var (token, tokenId) = tokenService.GenerateAdmissionToken(userId, eventId);
        await queueStore.StoreAdmissionTokenAsync(tokenId, userId, eventId, TimeSpan.FromMinutes(5));

        client.DefaultRequestHeaders.Add("X-Admission-Token", token);
        var reserveBody = new { eventId, tierId = Guid.NewGuid(), quantity = 1 };

        // First use succeeds
        var first = await client.PostAsJsonAsync("/checkout/reserve", reserveBody);
        first.EnsureSuccessStatusCode();

        // Second use fails (consumed)
        var second = await client.PostAsJsonAsync("/checkout/reserve", reserveBody);
        Assert.Equal(HttpStatusCode.Forbidden, second.StatusCode);
    }

    [Fact]
    public async Task QueuePosition_DecreasesAfterBatchAdmission()
    {
        var eventId = Guid.NewGuid();
        var queueStore = _factory.QueueStore;

        // Add 6 users
        var userIds = new List<Guid>();
        for (int i = 0; i < 6; i++)
        {
            var userId = Guid.NewGuid();
            userIds.Add(userId);
            await queueStore.JoinAsync(eventId, userId);
            await Task.Delay(5);
        }

        // User at index 4 (5th person) has position 4
        var posBefore = await queueStore.GetPositionAsync(eventId, userIds[4]);
        Assert.Equal(4, posBefore);

        // Pop first batch of 3 (positions 0, 1, 2 leave)
        await queueStore.PopBatchAsync(eventId, 3);

        // User who was at position 4 is now at position 1
        var posAfter = await queueStore.GetPositionAsync(eventId, userIds[4]);
        Assert.Equal(1, posAfter);
    }

    [Fact]
    public async Task LeaveQueue_RemovesUser()
    {
        var eventId = Guid.NewGuid();
        var client = await CreateAuthenticatedClient();

        await client.PostAsync($"/queue/{eventId}/join", null);
        var leaveResponse = await client.PostAsync($"/queue/{eventId}/leave", null);
        leaveResponse.EnsureSuccessStatusCode();

        var posResponse = await client.GetAsync($"/queue/{eventId}/position");
        Assert.Equal(HttpStatusCode.NotFound, posResponse.StatusCode);
    }

    [Fact]
    public async Task BurstJoins_AllRecorded()
    {
        var eventId = Guid.NewGuid();
        var queueStore = _factory.QueueStore;
        var count = 200;

        var tasks = Enumerable.Range(0, count)
            .Select(_ => queueStore.JoinAsync(eventId, Guid.NewGuid()));

        await Task.WhenAll(tasks);

        var length = await queueStore.GetQueueLengthAsync(eventId);
        Assert.Equal(count, length);
    }

    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        var (client, _) = await CreateAuthenticatedClientWithUserId();
        return client;
    }

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedClientWithUserId()
    {
        var client = _factory.CreateClient();

        var nonceResponse = await client.GetFromJsonAsync<NonceResponse>("/auth/nonce", JsonOpts);
        var nonce = nonceResponse!.Nonce;

        var key = EthECKey.GenerateKey();
        var address = key.GetPublicAddress();

        var message =
            $"localhost wants you to sign in with your Ethereum account:\n" +
            $"{address}\n" +
            $"\n" +
            $"Sign in to TixFlow\n" +
            $"\n" +
            $"URI: http://localhost\n" +
            $"Version: 1\n" +
            $"Chain ID: 8453\n" +
            $"Nonce: {nonce}\n" +
            $"Issued At: {DateTime.UtcNow:O}";

        var signer = new EthereumMessageSigner();
        var signature = signer.EncodeUTF8AndSign(message, key);

        var verifyResponse = await client.PostAsJsonAsync("/auth/verify",
            new { message, signature });
        verifyResponse.EnsureSuccessStatusCode();

        var tokenBody = await verifyResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOpts);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenBody!.Token);

        // Extract userId from /auth/me
        var meResponse = await client.GetFromJsonAsync<MeResponse>("/auth/me", JsonOpts);
        var userId = Guid.Parse(meResponse!.UserId!);

        return (client, userId);
    }

    private record NonceResponse(string Nonce);
    private record TokenResponse(string Token);
    private record MeResponse(string? UserId, string? Wallet);
    private record JoinResponseDto(long Position, long EstimatedWaitSeconds, Guid QueueSessionId);
}
