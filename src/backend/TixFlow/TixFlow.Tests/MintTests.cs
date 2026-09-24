using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nethereum.Signer;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers;
using TixFlow.Workers.Contract;

namespace TixFlow.Tests;

public class MintTests : IClassFixture<TixFlowWebFactory>
{
    private readonly TixFlowWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public MintTests(TixFlowWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConfirmOrder_CreatesMintJobs()
    {
        var (client, userId, eventId, orderId) = await ReserveTickets(quantity: 2);

        var confirmResponse = await client.PostAsJsonAsync("/checkout/confirm",
            new { orderId, txHash = "0x" + new string('a', 64) });
        confirmResponse.EnsureSuccessStatusCode();

        var body = await confirmResponse.Content.ReadFromJsonAsync<ConfirmResponse>(JsonOpts);
        Assert.Equal("Order confirmed. Minting queued.", body!.Message);
        Assert.Equal(2, body.MintJobsCreated);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var jobs = await db.MintJobs.Where(j => j.Ticket.OrderId == orderId).ToListAsync();
        Assert.Equal(2, jobs.Count);
        Assert.All(jobs, j => Assert.Equal(MintJobStatus.Pending, j.Status));
    }

    [Fact]
    public async Task ConfirmOrder_AlreadyConfirmed_ReturnsConflict()
    {
        var (client, _, _, orderId) = await ReserveTickets(quantity: 1);

        var first = await client.PostAsJsonAsync("/checkout/confirm",
            new { orderId, txHash = "0x" + new string('b', 64) });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/checkout/confirm",
            new { orderId, txHash = "0x" + new string('c', 64) });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ConfirmOrder_WrongUser_ReturnsForbid()
    {
        var (_, _, _, orderId) = await ReserveTickets(quantity: 1);

        var otherClient = await CreateAuthenticatedClient();
        var response = await otherClient.PostAsJsonAsync("/checkout/confirm",
            new { orderId, txHash = "0x" + new string('d', 64) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MintWorker_ProcessesPendingJobs()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            WalletAddress = "0x1234567890abcdef1234567890abcdef12345678",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Test Event",
            OrganizerId = user.Id,
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
            BuyerId = user.Id,
            EventId = evt.Id,
            TicketTierId = tier.Id,
            Quantity = 2,
            Status = OrderStatus.Confirmed,
            IdempotencyKey = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Orders.Add(order);

        var tickets = Enumerable.Range(0, 2).Select(_ => new Ticket
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            TicketTierId = tier.Id,
            OwnerId = user.Id,
            Status = TicketStatus.PendingMint,
            CreatedAt = DateTimeOffset.UtcNow
        }).ToList();
        db.Tickets.AddRange(tickets);

        var now = DateTimeOffset.UtcNow;
        var jobs = tickets.Select(t => new MintJob
        {
            Id = Guid.NewGuid(),
            TicketId = t.Id,
            Status = MintJobStatus.Pending,
            Attempts = 0,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();
        db.MintJobs.AddRange(jobs);
        await db.SaveChangesAsync();

        var fakeMintClient = new FakeMintClient();
        var fakeNonceService = new FakeNonceService();
        var options = Options.Create(new MintOptions
        {
            PollIntervalSeconds = 1,
            MaxBatchSize = 20,
            MaxAttempts = 3
        });

        var worker = new MintWorker(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            fakeMintClient,
            fakeNonceService,
            options,
            NullLogger<MintWorker>.Instance);

        await worker.ProcessPendingJobsAsync(CancellationToken.None);

        var jobIds = jobs.Select(j => j.Id).ToList();
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var updatedJobs = await verifyDb.MintJobs
            .Where(j => jobIds.Contains(j.Id))
            .ToListAsync();

        Assert.All(updatedJobs, j =>
        {
            Assert.Equal(MintJobStatus.Submitted, j.Status);
            Assert.NotNull(j.TxHash);
            Assert.NotNull(j.Nonce);
        });

        Assert.True(fakeMintClient.BatchCalls > 0 || fakeMintClient.SingleCalls > 0);
    }

    [Fact]
    public async Task MintWorker_RetriesAndFails_AfterMaxAttempts()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            WalletAddress = "0xdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Fail Event",
            OrganizerId = user.Id,
            StartsAt = DateTimeOffset.UtcNow.AddDays(30),
            VenueName = "Fail Venue"
        };
        db.Events.Add(evt);

        var tier = new TicketTier
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Name = "VIP",
            PriceUsdc = 100m,
            TotalSupply = 50
        };
        db.TicketTiers.Add(tier);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = user.Id,
            EventId = evt.Id,
            TicketTierId = tier.Id,
            Quantity = 1,
            Status = OrderStatus.Confirmed,
            IdempotencyKey = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Orders.Add(order);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            TicketTierId = tier.Id,
            OwnerId = user.Id,
            Status = TicketStatus.PendingMint,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Tickets.Add(ticket);

        var job = new MintJob
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = MintJobStatus.Pending,
            Attempts = 2,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.MintJobs.Add(job);
        await db.SaveChangesAsync();

        var failingClient = new FakeMintClient { ShouldFail = true };
        var fakeNonceService = new FakeNonceService();
        var options = Options.Create(new MintOptions
        {
            PollIntervalSeconds = 1,
            MaxBatchSize = 20,
            MaxAttempts = 3
        });

        var worker = new MintWorker(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            failingClient,
            fakeNonceService,
            options,
            NullLogger<MintWorker>.Instance);

        await worker.ProcessPendingJobsAsync(CancellationToken.None);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var updatedJob = await verifyDb.MintJobs.FindAsync(job.Id);
        Assert.Equal(MintJobStatus.Failed, updatedJob!.Status);
        Assert.Equal(3, updatedJob.Attempts);
        Assert.NotNull(updatedJob.LastError);
    }

    private async Task<(HttpClient client, Guid userId, Guid eventId, Guid orderId)> ReserveTickets(int quantity)
    {
        var (client, userId) = await CreateAuthenticatedClientWithUserId();
        var queueStore = _factory.QueueStore;

        var eventId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<Api.Queue.AdmissionTokenService>();
        var (token, tokenId) = tokenService.GenerateAdmissionToken(userId, eventId);
        await queueStore.StoreAdmissionTokenAsync(tokenId, userId, eventId, TimeSpan.FromMinutes(5));

        // Seed event and tier so FK constraints are met
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var existingUser = await db.Users.FindAsync(userId);
        if (existingUser is null)
        {
            db.Users.Add(new User
            {
                Id = userId,
                WalletAddress = "0x" + userId.ToString("N").PadRight(40, '0'),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        db.Events.Add(new Event
        {
            Id = eventId,
            Name = "Test Event",
            OrganizerId = userId,
            StartsAt = DateTimeOffset.UtcNow.AddDays(30),
            VenueName = "Test Venue"
        });

        var tierId = Guid.NewGuid();
        db.TicketTiers.Add(new TicketTier
        {
            Id = tierId,
            EventId = eventId,
            Name = "GA",
            PriceUsdc = 50m,
            TotalSupply = 100
        });
        await db.SaveChangesAsync();

        client.DefaultRequestHeaders.Add("X-Admission-Token", token);
        var reserveResponse = await client.PostAsJsonAsync("/checkout/reserve",
            new { eventId, tierId, quantity });
        reserveResponse.EnsureSuccessStatusCode();

        var reserveBody = await reserveResponse.Content.ReadFromJsonAsync<ReserveResponse>(JsonOpts);

        // Remove admission token header for subsequent requests
        client.DefaultRequestHeaders.Remove("X-Admission-Token");

        return (client, userId, eventId, reserveBody!.OrderId);
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

        var meResponse = await client.GetFromJsonAsync<MeResponse>("/auth/me", JsonOpts);
        var userId = Guid.Parse(meResponse!.UserId!);

        return (client, userId);
    }

    private record NonceResponse(string Nonce);
    private record TokenResponse(string Token);
    private record MeResponse(string? UserId, string? Wallet);
    private record ReserveResponse(string Message, Guid OrderId, Guid EventId, Guid TierId, int Quantity);
    private record ConfirmResponse(string Message, Guid OrderId, int MintJobsCreated);
}

public class FakeMintClient : IContractMintClient
{
    public int SingleCalls;
    public int BatchCalls;
    public bool ShouldFail;

    public Task<string> MintAsync(string toAddress, long nonce)
    {
        if (ShouldFail) throw new Exception("RPC error: simulated failure");
        Interlocked.Increment(ref SingleCalls);
        return Task.FromResult("0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
    }

    public Task<string> MintBatchAsync(string[] toAddresses, long nonce)
    {
        if (ShouldFail) throw new Exception("RPC error: simulated failure");
        Interlocked.Increment(ref BatchCalls);
        return Task.FromResult("0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
    }
}

public class FakeNonceService : INonceService
{
    private long _nonce;

    public Task InitializeAsync() => Task.CompletedTask;

    public long GetNextNonce() => Interlocked.Increment(ref _nonce) - 1;
}
