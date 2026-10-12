using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Contracts.EventEscrow.ContractDefinition;
using TixFlow.Infrastructure.Contracts.EventTicket.ContractDefinition;
using TixFlow.Infrastructure.Contracts.TicketFactory.ContractDefinition;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers;
using TixFlow.Workers.Contract;
using TixFlow.Workers.Indexer;

namespace TixFlow.Tests;

public class ChainFlowTests : IClassFixture<ChainEnabledFactory>
{
    private const string Contract = "0x00000000000000000000000000000000000000aa";
    private readonly ChainEnabledFactory _factory;

    public ChainFlowTests(ChainEnabledFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------------
    // The core guarantee: checkout never depends on the chain
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Checkout_Succeeds_WhileTheChainIsUnreachable()
    {
        var (client, userId, _) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory, Contract);

        var stopwatch = Stopwatch.StartNew();
        var orderId = await TestSupport.ReserveOrderAsync(_factory, client, userId, evt, quantity: 2);
        var confirm = await client.PostAsJsonAsync("/checkout/confirm",
            new { orderId, payment = TestSupport.PaymentAuthorization() });
        stopwatch.Stop();

        confirm.EnsureSuccessStatusCode();
        var body = await confirm.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("paidOnChain").GetBoolean());
        Assert.Equal(2, body.GetProperty("mintJobsCreated").GetInt32());

        // The configured RPC endpoint is dead and times out after 2 seconds. Finishing well inside that
        // proves neither request touched the chain.
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"checkout took {stopwatch.Elapsed}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var order = await db.Orders.Include(o => o.Tickets).SingleAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.True(order.HasPaymentAuthorization);
        Assert.All(order.Tickets, t => Assert.Equal(TicketStatus.PendingMint, t.Status));
        Assert.Equal(2, await db.MintJobs.CountAsync(j => j.Ticket.OrderId == orderId && j.Status == MintJobStatus.Pending));
    }

    [Fact]
    public async Task ChainHealth_ReportsDegraded_WithoutCallingTheChain()
    {
        var client = _factory.CreateClient();
        var stopwatch = Stopwatch.StartNew();
        var health = await client.GetFromJsonAsync<JsonElement>("/health/chain");
        stopwatch.Stop();

        Assert.Equal("Degraded", health.GetProperty("status").GetString());
        Assert.Contains("Checkout still works", health.GetProperty("message").GetString());
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ChainHealth_ReportsHealthy_WhenTheIndexerHeartbeatIsFresh()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
            var checkpoint = await db.IndexerCheckpoints.FirstOrDefaultAsync();
            if (checkpoint is null)
            {
                checkpoint = new Domain.Entities.IndexerCheckpoint { Id = 1, UpdatedAt = DateTimeOffset.UtcNow };
                db.IndexerCheckpoints.Add(checkpoint);
            }
            checkpoint.ChainHeadBlock = 1234;
            checkpoint.LastRpcSuccessAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        var health = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/health/chain");
        Assert.Equal("Healthy", health.GetProperty("status").GetString());
        Assert.Equal(1234, health.GetProperty("headBlock").GetInt64());

        using var cleanup = _factory.Services.CreateScope();
        var cleanupDb = cleanup.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        cleanupDb.IndexerCheckpoints.RemoveRange(cleanupDb.IndexerCheckpoints);
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Confirm_RejectsMalformedPaymentAuthorization()
    {
        var (client, userId, _) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory, Contract);
        var orderId = await TestSupport.ReserveOrderAsync(_factory, client, userId, evt, 1);

        var badNonce = await client.PostAsJsonAsync("/checkout/confirm", new
        {
            orderId,
            payment = new { validAfter = 0, validBefore = long.MaxValue / 2, nonce = "0x1234", signature = "0x" + new string('b', 130) }
        });
        Assert.Equal(HttpStatusCode.BadRequest, badNonce.StatusCode);

        var badSignature = await client.PostAsJsonAsync("/checkout/confirm", new
        {
            orderId,
            payment = new { validAfter = 0, validBefore = long.MaxValue / 2, nonce = "0x" + new string('a', 64), signature = "0xdead" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, badSignature.StatusCode);

        var expired = await client.PostAsJsonAsync("/checkout/confirm",
            new { orderId, payment = TestSupport.PaymentAuthorization(DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds()) });
        Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);

        // The order is still pending and can be confirmed properly afterwards.
        var ok = await client.PostAsJsonAsync("/checkout/confirm", new { orderId, payment = TestSupport.PaymentAuthorization() });
        ok.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Reserve_EnforcesThePerWalletLimit()
    {
        var (client, userId, _) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory, Contract, maxPerWallet: 3);

        (await TestSupport.ReserveAsync(_factory, client, userId, evt, 2)).EnsureSuccessStatusCode();

        var overLimit = await TestSupport.ReserveAsync(_factory, client, userId, evt, 2);
        Assert.Equal(HttpStatusCode.Conflict, overLimit.StatusCode);
        Assert.Contains("3 tickets per wallet", (await overLimit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());

        (await TestSupport.ReserveAsync(_factory, client, userId, evt, 1)).EnsureSuccessStatusCode();

        var full = await TestSupport.ReserveAsync(_factory, client, userId, evt, 1);
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        Assert.Contains("already holds the maximum", (await full.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }

    // ---------------------------------------------------------------------
    // Chain config and vouchers
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ChainConfig_ExposesWhatABrowserNeedsToVerifyOnChain()
    {
        var config = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/chain/config");

        Assert.True(config.GetProperty("enabled").GetBoolean());
        Assert.Equal(31337, config.GetProperty("chainId").GetInt64());
        Assert.Equal(ChainEnabledFactory.EscrowAddress, config.GetProperty("contracts").GetProperty("eventEscrow").GetString());
        Assert.Equal(ChainEnabledFactory.VoucherSignerAddress, config.GetProperty("voucherSigner").GetString(), ignoreCase: true);
    }

    [Fact]
    public async Task Voucher_ForConfirmedOrder_IsSignedByTheConfiguredSigner()
    {
        var (client, userId, key) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory, Contract);
        var orderId = await TestSupport.ReserveOrderAsync(_factory, client, userId, evt, 2);

        var beforeConfirm = await client.GetAsync($"/orders/{orderId}/voucher");
        Assert.Equal(HttpStatusCode.Conflict, beforeConfirm.StatusCode);

        (await client.PostAsJsonAsync("/checkout/confirm", new { orderId, payment = TestSupport.PaymentAuthorization() }))
            .EnsureSuccessStatusCode();

        var response = await client.GetFromJsonAsync<JsonElement>($"/orders/{orderId}/voucher");
        var voucher = response.GetProperty("voucher");

        Assert.Equal(Contract, response.GetProperty("ticketContract").GetString());
        Assert.Equal(key.GetPublicAddress(), voucher.GetProperty("buyer").GetString(), ignoreCase: true);
        Assert.Equal("1", voucher.GetProperty("tierId").GetString());
        Assert.Equal("2", voucher.GetProperty("quantity").GetString());
        Assert.Equal(ChainIds.OrderNonce(orderId).ToString(), voucher.GetProperty("nonce").GetString());
        Assert.Equal("25000000", response.GetProperty("unitPrice").GetString());
        Assert.Equal("0x" + new string('a', 64), response.GetProperty("payment").GetProperty("nonce").GetString());

        // Recover the signer from the typed-data signature exactly as a wallet library would.
        var typedData = VoucherSigner.BuildTypedData(Contract, 31337);
        var message = new MintVoucherMessage
        {
            Buyer = voucher.GetProperty("buyer").GetString()!,
            TierId = 1,
            Quantity = 2,
            Nonce = ChainIds.OrderNonce(orderId),
            Deadline = BigInteger.Parse(voucher.GetProperty("deadline").GetString()!)
        };
        var recovered = new Nethereum.Signer.EIP712.Eip712TypedDataSigner()
            .RecoverFromSignatureV4(message, typedData, response.GetProperty("signature").GetString());
        Assert.Equal(ChainEnabledFactory.VoucherSignerAddress, recovered, ignoreCase: true);
    }

    [Fact]
    public async Task Voucher_IsOnlyAvailableToTheBuyer()
    {
        var (buyer, buyerId, _) = await TestSupport.SignInAsync(_factory);
        var (other, _, _) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory, Contract);
        var orderId = await TestSupport.ReserveOrderAsync(_factory, buyer, buyerId, evt, 1);
        (await buyer.PostAsJsonAsync("/checkout/confirm", new { orderId })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/orders/{orderId}/voucher")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"/orders/{orderId}/voucher")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await buyer.GetAsync($"/orders/{Guid.NewGuid()}/voucher")).StatusCode);
    }

    [Fact]
    public async Task Voucher_IsRefusedUntilTheEventContractIsDeployed()
    {
        var (client, userId, _) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory, contractAddress: null);
        var orderId = await TestSupport.ReserveOrderAsync(_factory, client, userId, evt, 1);
        (await client.PostAsJsonAsync("/checkout/confirm", new { orderId })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync($"/orders/{orderId}/voucher")).StatusCode);
    }

    // ---------------------------------------------------------------------
    // Smart-wallet login (ERC-1271 / ERC-6492)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task SmartWalletLogin_SucceedsWhenTheValidatorAcceptsTheSignature()
    {
        const string wallet = "0x9999999999999999999999999999999999999999";
        _factory.SmartWallets.ValidSigners.Add(wallet);
        var client = _factory.CreateClient();

        var nonce = (await client.GetFromJsonAsync<TestSupport.NonceDto>("/auth/nonce", TestSupport.Json))!.Nonce;
        // An ERC-6492-style signature: far longer than 65 bytes, so ecrecover cannot apply.
        var signature = "0x" + new string('c', 400);

        var verify = await client.PostAsJsonAsync("/auth/verify", new { message = TestSupport.SiweMessage(wallet, nonce), signature });

        verify.EnsureSuccessStatusCode();
        Assert.True(_factory.SmartWallets.Calls > 0);
    }

    [Fact]
    public async Task SmartWalletLogin_FailsWhenTheValidatorRejectsTheSignature()
    {
        const string wallet = "0x8888888888888888888888888888888888888888";
        var client = _factory.CreateClient();
        var nonce = (await client.GetFromJsonAsync<TestSupport.NonceDto>("/auth/nonce", TestSupport.Json))!.Nonce;

        var verify = await client.PostAsJsonAsync("/auth/verify",
            new { message = TestSupport.SiweMessage(wallet, nonce), signature = "0x" + new string('c', 400) });

        Assert.Equal(HttpStatusCode.Unauthorized, verify.StatusCode);
    }

    [Fact]
    public async Task EoaLogin_NeverConsultsTheChain()
    {
        var before = _factory.SmartWallets.Calls;
        await TestSupport.SignInAsync(_factory);
        Assert.Equal(before, _factory.SmartWallets.Calls);
    }

    // ---------------------------------------------------------------------
    // Mint worker paths
    // ---------------------------------------------------------------------

    [Fact]
    public async Task MintWorker_RelaysPaidOrdersThroughTheEscrow()
    {
        var evt = await TestSupport.SeedEventAsync(_factory, Contract);
        var (orderId, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 2, paidOnChain: true);
        var mintClient = new FakeMintClient();

        await CreateMintWorker(mintClient).ProcessPendingJobsAsync(CancellationToken.None);

        var purchase = Assert.Single(mintClient.Purchases, p => p.Order.OrderId == orderId);
        Assert.Equal(2, purchase.Order.Quantity);
        Assert.Equal(1, purchase.Order.TierId);
        Assert.Equal("0x" + new string('a', 64), purchase.Payment.Nonce);
        Assert.DoesNotContain(mintClient.MintedOrders, o => o.OrderId == orderId);
        await AssertJobsAsync(orderId, MintJobStatus.Submitted, hasTxHash: true);
    }

    [Fact]
    public async Task MintWorker_BatchesUnpaidOrdersOnTheMinterPath()
    {
        var evt = await TestSupport.SeedEventAsync(_factory, Contract);
        var (first, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 1);
        var (second, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 3);
        var mintClient = new FakeMintClient();

        await CreateMintWorker(mintClient).ProcessPendingJobsAsync(CancellationToken.None);

        Assert.Contains(mintClient.MintedOrders, o => o.OrderId == first && o.Quantity == 1);
        Assert.Contains(mintClient.MintedOrders, o => o.OrderId == second && o.Quantity == 3);
        Assert.DoesNotContain(mintClient.Purchases, p => p.Order.OrderId == first || p.Order.OrderId == second);
        await AssertJobsAsync(first, MintJobStatus.Submitted, hasTxHash: true);
        await AssertJobsAsync(second, MintJobStatus.Submitted, hasTxHash: true);
    }

    [Fact]
    public async Task MintWorker_DoesNotResubmitAnOrderTheBuyerAlreadyClaimed()
    {
        var evt = await TestSupport.SeedEventAsync(_factory, Contract);
        var (orderId, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 1, paidOnChain: true);
        var mintClient = new FakeMintClient { AlreadyMinted = true };

        await CreateMintWorker(mintClient).ProcessPendingJobsAsync(CancellationToken.None);

        Assert.DoesNotContain(mintClient.Purchases, p => p.Order.OrderId == orderId);
        await AssertJobsAsync(orderId, MintJobStatus.Submitted, hasTxHash: false);
    }

    [Fact]
    public async Task MintWorker_KeepsJobsPendingWithoutPenalty_WhenTheChainIsDown()
    {
        var evt = await TestSupport.SeedEventAsync(_factory, Contract);
        var (paid, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 1, paidOnChain: true);
        var (unpaid, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 1);
        var mintClient = new FakeMintClient { FailWith = new HttpRequestException("Connection refused") };
        var worker = CreateMintWorker(mintClient);

        for (var i = 0; i < 5; i++)
            await worker.ProcessPendingJobsAsync(CancellationToken.None);

        // Five failed ticks would have exhausted MaxAttempts (3) if outages counted as attempts.
        foreach (var orderId in new[] { paid, unpaid })
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
            var jobs = await db.MintJobs.Where(j => j.Ticket.OrderId == orderId).ToListAsync();
            Assert.All(jobs, j =>
            {
                Assert.Equal(MintJobStatus.Pending, j.Status);
                Assert.Equal(0, j.Attempts);
            });
        }

        // When the chain is back the same orders go through.
        mintClient.FailWith = null;
        await worker.ProcessPendingJobsAsync(CancellationToken.None);
        await AssertJobsAsync(paid, MintJobStatus.Submitted, hasTxHash: true);
        await AssertJobsAsync(unpaid, MintJobStatus.Submitted, hasTxHash: true);
    }

    [Fact]
    public async Task MintWorker_WaitsUntilTheEventContractIsDeployed()
    {
        var evt = await TestSupport.SeedEventAsync(_factory, contractAddress: null);
        var (orderId, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 1);
        var mintClient = new FakeMintClient();

        await CreateMintWorker(mintClient).ProcessPendingJobsAsync(CancellationToken.None);

        Assert.DoesNotContain(mintClient.MintedOrders, o => o.OrderId == orderId);
        await AssertJobsAsync(orderId, MintJobStatus.Pending, hasTxHash: false);
    }

    // ---------------------------------------------------------------------
    // Indexer handlers
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Indexer_EventCreated_FillsTheContractAddress()
    {
        var evt = await TestSupport.SeedEventAsync(_factory, contractAddress: null);
        const string deployed = "0xAbCdEf0000000000000000000000000000000001";

        await WithIndexerAsync((indexer, db) => indexer.ApplyEventCreatedAsync(db,
            new EventCreatedEventDTO { EventId = ChainIds.EventId(evt.EventId), Ticket = deployed, Organizer = Contract },
            Log(Contract), CancellationToken.None));

        using var scope = _factory.Services.CreateScope();
        var saved = await scope.ServiceProvider.GetRequiredService<TixFlowDbContext>().Events.SingleAsync(e => e.Id == evt.EventId);
        Assert.Equal(deployed.ToLowerInvariant(), saved.ContractAddress);
        Assert.NotNull(saved.DeployTxHash);
    }

    [Fact]
    public async Task Indexer_Minted_AssignsConsecutiveTokenIdsAndConfirmsJobs()
    {
        var contract = NewContract();
        var evt = await TestSupport.SeedEventAsync(_factory, contract);
        var (orderId, ticketIds, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 3);
        var log = Log(contract);

        // Jobs are still Pending, as they are when the buyer self-claims before the worker runs.
        await WithIndexerAsync((indexer, db) => indexer.ApplyMintedAsync(db,
            new MintedEventDTO { Buyer = Contract, TierId = 1, Nonce = ChainIds.OrderNonce(orderId), FirstTokenId = 41, Quantity = 3 },
            log, CancellationToken.None));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var tickets = await db.Tickets.Where(t => t.OrderId == orderId).OrderBy(t => t.Id).ToListAsync();
        Assert.Equal(ticketIds, tickets.Select(t => t.Id).ToList());
        Assert.Equal(new long?[] { 41, 42, 43 }, tickets.Select(t => t.TokenId).ToArray());
        Assert.All(tickets, t => Assert.Equal(TicketStatus.Minted, t.Status));

        var jobs = await db.MintJobs.Where(j => j.Ticket.OrderId == orderId).ToListAsync();
        Assert.All(jobs, j =>
        {
            Assert.Equal(MintJobStatus.Confirmed, j.Status);
            Assert.Equal(log.TxHash, j.TxHash);
        });
        Assert.Single(await db.ChainActivities.Where(a => a.TxHash == log.TxHash).ToListAsync());
    }

    [Fact]
    public async Task Indexer_ReplayingALog_IsIdempotent()
    {
        var contract = NewContract();
        var evt = await TestSupport.SeedEventAsync(_factory, contract);
        var (orderId, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 2);
        var log = Log(contract);
        var minted = new MintedEventDTO { Buyer = Contract, TierId = 1, Nonce = ChainIds.OrderNonce(orderId), FirstTokenId = 7, Quantity = 2 };

        for (var i = 0; i < 3; i++)
            await WithIndexerAsync((indexer, db) => indexer.ApplyMintedAsync(db, minted, log, CancellationToken.None));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        Assert.Equal(new long?[] { 7, 8 },
            (await db.Tickets.Where(t => t.OrderId == orderId).OrderBy(t => t.Id).ToListAsync()).Select(t => t.TokenId).ToArray());
        Assert.Equal(1, await db.ChainActivities.CountAsync(a => a.TxHash == log.TxHash));
    }

    [Fact]
    public async Task Indexer_OrderRejected_FailsTheJobs_ButIgnoresAlreadyMintedOrders()
    {
        var contract = NewContract();
        var evt = await TestSupport.SeedEventAsync(_factory, contract);
        var (rejected, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 1);
        var (alreadyMinted, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 1);

        await WithIndexerAsync(async (indexer, db) =>
        {
            await indexer.ApplyOrderRejectedAsync(db,
                new OrderRejectedEventDTO { Buyer = Contract, Nonce = ChainIds.OrderNonce(rejected), Reason = 6 },
                Log(contract), CancellationToken.None);
            await indexer.ApplyOrderRejectedAsync(db,
                new OrderRejectedEventDTO { Buyer = Contract, Nonce = ChainIds.OrderNonce(alreadyMinted), Reason = 1 },
                Log(contract), CancellationToken.None);
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var failed = await db.MintJobs.SingleAsync(j => j.Ticket.OrderId == rejected);
        Assert.Equal(MintJobStatus.Failed, failed.Status);
        Assert.Contains("MaxPerWalletExceeded", failed.LastError);
        Assert.Equal(MintJobStatus.Pending, (await db.MintJobs.SingleAsync(j => j.Ticket.OrderId == alreadyMinted)).Status);
    }

    [Fact]
    public async Task Indexer_Transfer_UpdatesTheOwner_ButNotWhileListedOnTheMarketplace()
    {
        const string marketplace = "0x5FC8d32690cc91D4c39d9d3abcBD16989F875707";
        const string newOwner = "0x7777777777777777777777777777777777777777";
        var contract = NewContract();
        var evt = await TestSupport.SeedEventAsync(_factory, contract);
        var (orderId, _, sellerId) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 1);
        var chain = new ChainOptions { Contracts = { PriceCappedResale = marketplace } };

        await WithIndexerAsync(async (indexer, db) =>
        {
            await indexer.ApplyMintedAsync(db,
                new MintedEventDTO { Buyer = Contract, TierId = 1, Nonce = ChainIds.OrderNonce(orderId), FirstTokenId = 5, Quantity = 1 },
                Log(contract), CancellationToken.None);
            await db.SaveChangesAsync();

            // Listing: the ticket moves into the marketplace's escrow, the seller still owns it.
            await indexer.ApplyTransferAsync(db,
                new TransferEventDTO { From = Contract, To = marketplace, TokenId = 5 }, Log(contract), CancellationToken.None);
            await db.SaveChangesAsync();
            Assert.Equal(sellerId, (await db.Tickets.SingleAsync(t => t.OrderId == orderId)).OwnerId);

            // Sale: the marketplace delivers it to a wallet that has never signed in.
            await indexer.ApplyTransferAsync(db,
                new TransferEventDTO { From = marketplace, To = newOwner, TokenId = 5 }, Log(contract), CancellationToken.None);
        }, chain);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var ticket = await db.Tickets.Include(t => t.Owner).SingleAsync(t => t.OrderId == orderId);
        Assert.NotEqual(sellerId, ticket.OwnerId);
        Assert.Equal(newOwner, ticket.Owner.WalletAddress);
    }

    [Fact]
    public async Task Indexer_TracksRedemptionCancellationRefundAndRelease()
    {
        var contract = NewContract();
        var evt = await TestSupport.SeedEventAsync(_factory, contract);
        var (orderId, _, _) = await TestSupport.SeedConfirmedOrderAsync(_factory, evt, 1, paidOnChain: true);
        var root = MerkleTree.Leaf(9);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
            db.RedemptionCheckpoints.Add(new Domain.Entities.RedemptionCheckpoint
            {
                Id = Guid.NewGuid(), EventId = evt.EventId, Root = root.ToHex(true), Count = 1, CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await WithIndexerAsync(async (indexer, db) =>
        {
            var ct = CancellationToken.None;
            await indexer.ApplyMintedAsync(db,
                new MintedEventDTO { Buyer = Contract, TierId = 1, Nonce = ChainIds.OrderNonce(orderId), FirstTokenId = 9, Quantity = 1 },
                Log(contract), ct);
            await db.SaveChangesAsync();

            await indexer.ApplyPaymentDepositedAsync(db,
                new PaymentDepositedEventDTO { Ticket = contract, Buyer = Contract, Payer = Contract, FirstTokenId = 9, Quantity = 1, Amount = 25_000_000 },
                Log(ChainEnabledFactory.EscrowAddress), ct);
            await indexer.ApplyCheckpointedAsync(db,
                new RedemptionsCheckpointedEventDTO { Root = root, Count = 1, CheckpointId = 1 }, Log(contract), ct);
            await indexer.ApplyRedeemedAsync(db, new RedeemedEventDTO { TokenId = 9, Holder = Contract }, Log(contract), ct);
            await indexer.ApplyEventCancelledAsync(db,
                new EventCancelledEventDTO { Ticket = contract, CancelledBy = Contract }, Log(ChainEnabledFactory.EscrowAddress), ct);
            await indexer.ApplyRefundedAsync(db,
                new RefundedEventDTO { Ticket = contract, TokenId = 9, Holder = Contract, Amount = 25_000_000 },
                Log(ChainEnabledFactory.EscrowAddress), ct);
            await indexer.ApplyEscrowReleasedAsync(db,
                new EscrowReleasedEventDTO { Ticket = contract, Organizer = Contract, Amount = 0 }, Log(ChainEnabledFactory.EscrowAddress), ct);
        });

        using var verify = _factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var ticket = await verifyDb.Tickets.SingleAsync(t => t.OrderId == orderId);
        var savedEvent = await verifyDb.Events.SingleAsync(e => e.Id == evt.EventId);
        var checkpoint = await verifyDb.RedemptionCheckpoints.SingleAsync(c => c.EventId == evt.EventId);

        Assert.NotNull(ticket.BadgeClaimedAt);
        Assert.NotNull(ticket.RefundedAt);
        Assert.NotNull(savedEvent.CancelledAt);
        Assert.NotNull(savedEvent.EscrowReleasedAt);
        Assert.NotNull(checkpoint.ConfirmedAt);

        var kinds = await verifyDb.ChainActivities.Where(a => a.EventId == evt.EventId).Select(a => a.Kind).ToListAsync();
        Assert.Equal(
            new[] { "BadgeClaimed", "EscrowReleased", "EventCancelled", "Minted", "PaymentDeposited", "RedemptionsCheckpointed", "Refunded" },
            kinds.Order().ToArray());

        var feed = await _factory.CreateClient().GetFromJsonAsync<JsonElement>($"/events/{evt.EventId}/activity");
        Assert.Equal(7, feed.GetArrayLength());
    }

    [Fact]
    public async Task AttendanceProof_VerifiesAgainstTheCheckpointedRoot()
    {
        var contract = NewContract();
        var (client, userId, _) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory, contract);
        var orderId = await TestSupport.ReserveOrderAsync(_factory, client, userId, evt, 3);
        (await client.PostAsJsonAsync("/checkout/confirm", new { orderId })).EnsureSuccessStatusCode();

        Guid ticketId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
            var tickets = await db.Tickets.Where(t => t.OrderId == orderId).OrderBy(t => t.Id).ToListAsync();
            for (var i = 0; i < tickets.Count; i++)
            {
                tickets[i].TokenId = 100 + i;
                tickets[i].Status = TicketStatus.Redeemed;
                tickets[i].RedeemedAt = DateTimeOffset.UtcNow;
            }
            // Only the first two have been included in a checkpoint so far.
            tickets[0].CheckpointedAt = tickets[1].CheckpointedAt = DateTimeOffset.UtcNow;
            ticketId = tickets[1].Id;
            await db.SaveChangesAsync();

            Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync($"/tickets/{tickets[2].Id}/attendance-proof")).StatusCode);
        }

        var response = await client.GetFromJsonAsync<JsonElement>($"/tickets/{ticketId}/attendance-proof");
        var proof = response.GetProperty("proof").EnumerateArray().Select(p => p.GetString()!.HexToByteArray()).ToList();
        var root = response.GetProperty("root").GetString()!.HexToByteArray();

        Assert.Equal("101", response.GetProperty("tokenId").GetString());
        Assert.Equal(new MerkleTree([100, 101]).Root, root);
        Assert.True(MerkleTree.Verify(proof, root, 101));

        var (stranger, _, _) = await TestSupport.SignInAsync(_factory);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/tickets/{ticketId}/attendance-proof")).StatusCode);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static int _contractCounter;
    private static int _logCounter;

    private static string NewContract() =>
        "0x" + Interlocked.Increment(ref _contractCounter).ToString("x").PadLeft(40, 'c');

    private static LogContext Log(string contract) =>
        new(contract.ToLowerInvariant(), "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            Interlocked.Increment(ref _logCounter), 100);

    private MintWorker CreateMintWorker(IContractMintClient mintClient) => new(
        _factory.Services.GetRequiredService<IServiceScopeFactory>(),
        mintClient,
        Options.Create(new MintOptions { MaxBatchSize = 500, MaxAttempts = 3 }),
        NullLogger<MintWorker>.Instance);

    private async Task WithIndexerAsync(Func<ChainIndexer, TixFlowDbContext, Task> action, ChainOptions? chain = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var indexer = new ChainIndexer(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            new FakeWeb3Provider(blockHash: "0x", blockNumber: 0),
            Options.Create(new IndexerOptions()),
            Options.Create(new MintOptions()),
            Options.Create(chain ?? new ChainOptions()),
            NullLogger<ChainIndexer>.Instance);

        await action(indexer, db);
        await db.SaveChangesAsync();
    }

    private async Task AssertJobsAsync(Guid orderId, MintJobStatus status, bool hasTxHash)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var jobs = await db.MintJobs.Where(j => j.Ticket.OrderId == orderId).ToListAsync();
        Assert.NotEmpty(jobs);
        Assert.All(jobs, j =>
        {
            Assert.Equal(status, j.Status);
            Assert.Equal(hasTxHash, j.TxHash is not null);
        });
    }
}
