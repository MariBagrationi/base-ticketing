using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nethereum.Signer;
using Nethereum.Web3;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers.Contract;
using TixFlow.Workers.Indexer;

namespace TixFlow.Tests;

public class IndexerTests : IClassFixture<TixFlowWebFactory>
{
    private readonly TixFlowWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public IndexerTests(TixFlowWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReorgDetection_RevertsConfirmedJobsToSubmitted()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            WalletAddress = "0xaabbccddaabbccddaabbccddaabbccddaabbccdd",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Reorg Event",
            OrganizerId = user.Id,
            StartsAt = DateTimeOffset.UtcNow.AddDays(30),
            VenueName = "Reorg Venue"
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
            Status = TicketStatus.Minted,
            TokenId = 42,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Tickets.Add(ticket);

        var mintJob = new MintJob
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = MintJobStatus.Confirmed,
            TxHash = "0x" + new string('f', 64),
            Nonce = 1,
            Attempts = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.MintJobs.Add(mintJob);

        var checkpoint = new IndexerCheckpoint
        {
            LastProcessedBlock = 100,
            BlockHash = "0xOriginalHash",
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.IndexerCheckpoints.Add(checkpoint);
        await db.SaveChangesAsync();

        var fakeWeb3 = new FakeWeb3Provider(blockHash: "0xDIFFERENT_HASH", blockNumber: 110);
        var indexerOptions = Options.Create(new IndexerOptions
        {
            PollIntervalSeconds = 1,
            ConfirmationBlocks = 2,
            ReorgRewindBlocks = 15,
            ContractDeployBlock = 1,
            StuckTxTimeoutMinutes = 5
        });
        var mintOptions = Options.Create(new MintOptions
        {
            ContractAddress = "0x0000000000000000000000000000000000000001",
            MaxAttempts = 3
        });

        var indexer = new ChainIndexer(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            fakeWeb3,
            indexerOptions,
            mintOptions,
            NullLogger<ChainIndexer>.Instance);

        var reorged = await indexer.DetectAndHandleReorgAsync(db, checkpoint, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(reorged);
        Assert.True(checkpoint.LastProcessedBlock < 100);
        Assert.Null(checkpoint.BlockHash);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var updatedJob = await verifyDb.MintJobs.FindAsync(mintJob.Id);
        Assert.Equal(MintJobStatus.Submitted, updatedJob!.Status);

        var updatedTicket = await verifyDb.Tickets.FindAsync(ticket.Id);
        Assert.Equal(TicketStatus.PendingMint, updatedTicket!.Status);
        Assert.Null(updatedTicket.TokenId);
    }

    [Fact]
    public async Task NoReorg_WhenBlockHashMatches()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var checkpoint = new IndexerCheckpoint
        {
            LastProcessedBlock = 200,
            BlockHash = "0xSAME_HASH",
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.IndexerCheckpoints.Add(checkpoint);
        await db.SaveChangesAsync();

        var fakeWeb3 = new FakeWeb3Provider(blockHash: "0xSAME_HASH", blockNumber: 210);
        var indexerOptions = Options.Create(new IndexerOptions
        {
            ContractDeployBlock = 1,
            ReorgRewindBlocks = 15
        });
        var mintOptions = Options.Create(new MintOptions
        {
            ContractAddress = "0x0000000000000000000000000000000000000001"
        });

        var indexer = new ChainIndexer(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            fakeWeb3,
            indexerOptions,
            mintOptions,
            NullLogger<ChainIndexer>.Instance);

        var reorged = await indexer.DetectAndHandleReorgAsync(db, checkpoint, CancellationToken.None);

        Assert.False(reorged);
        Assert.Equal(200, checkpoint.LastProcessedBlock);
    }

    [Fact]
    public async Task StuckTransactions_ResetToPending()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            WalletAddress = "0xstuckstuckstuckstuckstuckstuckstuckstuck",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Stuck Event",
            OrganizerId = user.Id,
            StartsAt = DateTimeOffset.UtcNow.AddDays(30),
            VenueName = "Stuck Venue"
        };
        db.Events.Add(evt);

        var tier = new TicketTier
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Name = "GA",
            PriceUsdc = 25m,
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

        var stuckJob = new MintJob
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = MintJobStatus.Submitted,
            TxHash = "0x" + new string('e', 64),
            Nonce = 5,
            Attempts = 1,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
        db.MintJobs.Add(stuckJob);
        await db.SaveChangesAsync();

        var indexerOptions = Options.Create(new IndexerOptions { StuckTxTimeoutMinutes = 5 });
        var mintOptions = Options.Create(new MintOptions
        {
            ContractAddress = "0x0000000000000000000000000000000000000001",
            MaxAttempts = 3
        });

        var indexer = new ChainIndexer(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            new FakeWeb3Provider(blockHash: "0x", blockNumber: 0),
            indexerOptions,
            mintOptions,
            NullLogger<ChainIndexer>.Instance);

        await indexer.HandleStuckTransactionsAsync(db, CancellationToken.None);
        await db.SaveChangesAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var updated = await verifyDb.MintJobs.FindAsync(stuckJob.Id);
        Assert.Equal(MintJobStatus.Pending, updated!.Status);
        Assert.Contains("Stuck transaction", updated.LastError);
    }

    [Fact]
    public async Task StuckTransactions_NotReset_WhenMaxAttemptsReached()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            WalletAddress = "0xmaxedmaxedmaxedmaxedmaxedmaxedmaxedmaxed",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Maxed Event",
            OrganizerId = user.Id,
            StartsAt = DateTimeOffset.UtcNow.AddDays(30),
            VenueName = "Maxed Venue"
        };
        db.Events.Add(evt);

        var tier = new TicketTier
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Name = "GA",
            PriceUsdc = 25m,
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

        var maxedJob = new MintJob
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Status = MintJobStatus.Submitted,
            TxHash = "0x" + new string('d', 64),
            Nonce = 6,
            Attempts = 3,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
        db.MintJobs.Add(maxedJob);
        await db.SaveChangesAsync();

        var indexerOptions = Options.Create(new IndexerOptions { StuckTxTimeoutMinutes = 5 });
        var mintOptions = Options.Create(new MintOptions
        {
            ContractAddress = "0x0000000000000000000000000000000000000001",
            MaxAttempts = 3
        });

        var indexer = new ChainIndexer(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            new FakeWeb3Provider(blockHash: "0x", blockNumber: 0),
            indexerOptions,
            mintOptions,
            NullLogger<ChainIndexer>.Instance);

        await indexer.HandleStuckTransactionsAsync(db, CancellationToken.None);
        await db.SaveChangesAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var updated = await verifyDb.MintJobs.FindAsync(maxedJob.Id);
        Assert.Equal(MintJobStatus.Submitted, updated!.Status);
    }

    [Fact]
    public async Task GetTicket_ReturnsTicketStatus()
    {
        var (client, userId) = await CreateAuthenticatedClientWithUserId();

        using var scope = _factory.Services.CreateScope();
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

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Ticket Status Event",
            OrganizerId = userId,
            StartsAt = DateTimeOffset.UtcNow.AddDays(30),
            VenueName = "Status Venue"
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
            Status = TicketStatus.Minted,
            TokenId = 99,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var response = await client.GetAsync($"/tickets/{ticketId}");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOpts);
        Assert.Equal(ticketId, body!.Id);
        Assert.Equal("Minted", body.Status);
        Assert.Equal(99, body.TokenId);
    }

    [Fact]
    public async Task GetTicket_OtherUser_Returns404()
    {
        var (client1, userId1) = await CreateAuthenticatedClientWithUserId();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var otherUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = otherUserId,
            WalletAddress = "0x" + otherUserId.ToString("N").PadRight(40, '0'),
            CreatedAt = DateTimeOffset.UtcNow
        });

        var existingUser = await db.Users.FindAsync(userId1);
        if (existingUser is null)
        {
            db.Users.Add(new User
            {
                Id = userId1,
                WalletAddress = "0x" + userId1.ToString("N").PadRight(40, '0'),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Other User Event",
            OrganizerId = otherUserId,
            StartsAt = DateTimeOffset.UtcNow.AddDays(30),
            VenueName = "Other Venue"
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
            BuyerId = otherUserId,
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
            OwnerId = otherUserId,
            Status = TicketStatus.Minted,
            TokenId = 77,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var response = await client1.GetAsync($"/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTicket_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/tickets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    private record TicketResponse(Guid Id, string Status, long? TokenId, Guid OrderId, Guid TicketTierId);
}

public class FakeWeb3Provider : IWeb3Provider
{
    private readonly string? _blockHash;
    private readonly long _blockNumber;

    public FakeWeb3Provider(string? blockHash, long blockNumber)
    {
        _blockHash = blockHash;
        _blockNumber = blockNumber;
    }

    public IWeb3 GetWeb3() =>
        throw new NotSupportedException("FakeWeb3Provider does not support full IWeb3");

    public Task<long> GetBlockNumberAsync() => Task.FromResult(_blockNumber);

    public Task<string?> GetBlockHashAtAsync(long blockNumber) => Task.FromResult(_blockHash);
}
