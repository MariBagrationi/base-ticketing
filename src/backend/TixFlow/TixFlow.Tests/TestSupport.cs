using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Signer;
using TixFlow.Api.Queue;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Tests;

/// <summary>A test host with a chain configured but unreachable, and no background workers.</summary>
public class ChainEnabledFactory : TixFlowWebFactory
{
    // Public Anvil development key (account #2). Local tests only.
    public const string VoucherSignerKey = "0x5de4111afa1a4b94908f83103eb1f1706367c2e68ca870fc3fb9a804cdab365a";
    public const string VoucherSignerAddress = "0x3C44CdDdB6a900fa2b585dd299e03d12FA4293BC";
    public const string EscrowAddress = "0xDc64a140Aa3E981100a9becA4E685f962f0cF6C9";

    public FakeSmartWalletVerifier SmartWallets { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("Chain:Enabled", "true");
        builder.UseSetting("Chain:ChainId", "31337");
        // Nothing listens here: any chain call made during a request would fail or hang the test.
        builder.UseSetting("Chain:RpcUrl", "http://127.0.0.1:9");
        builder.UseSetting("Chain:RpcTimeoutSeconds", "2");
        builder.UseSetting("Chain:VoucherSignerPrivateKey", VoucherSignerKey);
        builder.UseSetting("Chain:Contracts:EventEscrow", EscrowAddress);
        builder.UseSetting("Chain:Contracts:Usdc", "0x5FbDB2315678afecb367f032d93F642f64180aa3");
        builder.UseSetting("Chain:Contracts:TicketFactory", "0x0165878A594ca255338adfa4d48449f69242Eb8F");
        builder.UseSetting("HostedWorkers:Enabled", "false");

        builder.ConfigureServices(services =>
        {
            var existing = services.Single(d => d.ServiceType == typeof(ISmartWalletSignatureVerifier));
            services.Remove(existing);
            services.AddSingleton<ISmartWalletSignatureVerifier>(SmartWallets);
        });
    }
}

public class FakeSmartWalletVerifier : ISmartWalletSignatureVerifier
{
    public HashSet<string> ValidSigners { get; } = new(StringComparer.OrdinalIgnoreCase);
    public int Calls;

    public bool IsAvailable => true;

    public Task<bool> IsValidAsync(string signer, byte[] hash, byte[] signature)
    {
        Interlocked.Increment(ref Calls);
        return Task.FromResult(ValidSigners.Contains(signer));
    }
}

public record SeededEvent(Guid EventId, Guid TierId, string? ContractAddress);

public static class TestSupport
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static string SiweMessage(string address, string nonce) =>
        "localhost wants you to sign in with your Ethereum account:\n" +
        $"{address}\n\nSign in to TixFlow\n\n" +
        "URI: http://localhost\nVersion: 1\nChain ID: 31337\n" +
        $"Nonce: {nonce}\nIssued At: {DateTime.UtcNow:O}";

    public static async Task<(HttpClient Client, Guid UserId, EthECKey Key)> SignInAsync(TixFlowWebFactory factory, EthECKey? key = null)
    {
        key ??= EthECKey.GenerateKey();
        var client = factory.CreateClient();

        var nonce = (await client.GetFromJsonAsync<NonceDto>("/auth/nonce", Json))!.Nonce;
        var message = SiweMessage(key.GetPublicAddress(), nonce);
        var signature = new EthereumMessageSigner().EncodeUTF8AndSign(message, key);

        var verify = await client.PostAsJsonAsync("/auth/verify", new { message, signature });
        verify.EnsureSuccessStatusCode();
        var token = (await verify.Content.ReadFromJsonAsync<TokenDto>(Json))!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var me = await client.GetFromJsonAsync<MeDto>("/auth/me", Json);
        return (client, Guid.Parse(me!.UserId), key);
    }

    public static async Task<SeededEvent> SeedEventAsync(
        TixFlowWebFactory factory,
        string? contractAddress = null,
        decimal price = 25m,
        int supply = 100,
        int maxPerWallet = TicketTier.DefaultMaxPerWallet)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();

        var organizer = new User
        {
            Id = Guid.NewGuid(),
            WalletAddress = "0x" + Guid.NewGuid().ToString("N") + "00000000",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Seeded Event",
            VenueName = "Test Hall",
            StartsAt = DateTimeOffset.UtcNow.AddDays(10),
            EndsAt = DateTimeOffset.UtcNow.AddDays(10).AddHours(6),
            OrganizerId = organizer.Id,
            ContractAddress = contractAddress?.ToLowerInvariant()
        };
        var tier = new TicketTier
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Name = "General Admission",
            PriceUsdc = price,
            TotalSupply = supply,
            OnChainTierId = 1,
            MaxPerWallet = maxPerWallet
        };
        db.AddRange(organizer, evt, tier);
        await db.SaveChangesAsync();
        return new SeededEvent(evt.Id, tier.Id, evt.ContractAddress);
    }

    /// <summary>Issues an admission token directly, standing in for the waiting room.</summary>
    public static async Task<string> AdmitAsync(TixFlowWebFactory factory, Guid userId, Guid eventId)
    {
        using var scope = factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<AdmissionTokenService>();
        var (token, tokenId) = tokens.GenerateAdmissionToken(userId, eventId);
        await factory.QueueStore.StoreAdmissionTokenAsync(tokenId, userId, eventId, TimeSpan.FromMinutes(5));
        return token;
    }

    public static async Task<HttpResponseMessage> ReserveAsync(
        TixFlowWebFactory factory, HttpClient client, Guid userId, SeededEvent evt, int quantity)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/checkout/reserve")
        {
            Content = JsonContent.Create(new { eventId = evt.EventId, tierId = evt.TierId, quantity })
        };
        request.Headers.Add("X-Admission-Token", await AdmitAsync(factory, userId, evt.EventId));
        return await client.SendAsync(request);
    }

    public static async Task<Guid> ReserveOrderAsync(
        TixFlowWebFactory factory, HttpClient client, Guid userId, SeededEvent evt, int quantity)
    {
        var response = await ReserveAsync(factory, client, userId, evt, quantity);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReserveDto>(Json))!.OrderId;
    }

    /// <summary>A structurally valid EIP-3009 authorisation. Its signature is never checked off-chain.</summary>
    public static object PaymentAuthorization(long? validBefore = null) => new
    {
        validAfter = 0,
        validBefore = validBefore ?? DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds(),
        nonce = "0x" + new string('a', 64),
        signature = "0x" + new string('b', 128) + "1b"
    };

    /// <summary>Creates a confirmed order with pending mint jobs directly in the database.</summary>
    public static async Task<(Guid OrderId, List<Guid> TicketIds, Guid BuyerId)> SeedConfirmedOrderAsync(
        TixFlowWebFactory factory, SeededEvent evt, int quantity, bool paidOnChain = false, string? buyerWallet = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var now = DateTimeOffset.UtcNow;

        var buyer = new User
        {
            Id = Guid.NewGuid(),
            WalletAddress = (buyerWallet ?? "0x" + Guid.NewGuid().ToString("N") + "11111111").ToLowerInvariant(),
            CreatedAt = now
        };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = buyer.Id,
            EventId = evt.EventId,
            TicketTierId = evt.TierId,
            Quantity = quantity,
            Status = OrderStatus.Confirmed,
            IdempotencyKey = Guid.NewGuid().ToString(),
            CreatedAt = now
        };
        if (paidOnChain)
        {
            order.PaymentValidAfter = 0;
            order.PaymentValidBefore = now.AddHours(24).ToUnixTimeSeconds();
            order.PaymentNonce = "0x" + new string('a', 64);
            order.PaymentSignature = "0x" + new string('b', 128) + "1b";
        }

        var tickets = Enumerable.Range(0, quantity).Select(_ => new Ticket
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            TicketTierId = evt.TierId,
            OwnerId = buyer.Id,
            Status = TicketStatus.PendingMint,
            CreatedAt = now
        }).ToList();
        var jobs = tickets.Select(t => new MintJob
        {
            Id = Guid.NewGuid(),
            TicketId = t.Id,
            Status = MintJobStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Add(buyer);
        db.Add(order);
        db.AddRange(tickets);
        db.AddRange(jobs);
        await db.SaveChangesAsync();
        return (order.Id, tickets.Select(t => t.Id).Order().ToList(), buyer.Id);
    }

    public record NonceDto(string Nonce);
    public record TokenDto(string Token);
    public record MeDto(string UserId, string Wallet);
    public record ReserveDto(Guid OrderId);
}
