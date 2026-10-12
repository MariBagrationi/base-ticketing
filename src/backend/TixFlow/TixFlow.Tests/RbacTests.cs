using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Signer;
using TixFlow.Domain.Entities;
using TixFlow.Domain.Enums;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Tests;

/// <summary>A test host with one wallet configured as a bootstrap admin through Auth:AdminWallets.</summary>
public class RbacFactory : TixFlowWebFactory
{
    public static readonly EthECKey AdminKey = EthECKey.GenerateKey();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Auth:AdminWallets", AdminKey.GetPublicAddress());
    }
}

public class RbacTests : IClassFixture<RbacFactory>
{
    private readonly RbacFactory _factory;

    public RbacTests(RbacFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------------
    // How roles are obtained
    // ---------------------------------------------------------------------

    [Fact]
    public async Task FirstLogin_GrantsTheUserRoleOnly()
    {
        var (client, _, _) = await TestSupport.SignInAsync(_factory);
        var me = await client.GetFromJsonAsync<JsonElement>("/auth/me");

        Assert.Equal(["User"], Roles(me));
    }

    [Fact]
    public async Task ConfiguredAdminWallet_GetsAdminOnLogin_AndItIsAudited()
    {
        var (client, _, key) = await TestSupport.SignInAsync(_factory, RbacFactory.AdminKey);
        var me = await client.GetFromJsonAsync<JsonElement>("/auth/me");

        Assert.Equal(["Admin", "User"], Roles(me));

        var audit = await client.GetFromJsonAsync<JsonElement>("/admin/audit-log");
        Assert.Contains(audit.EnumerateArray(), e =>
            e.GetProperty("action").GetString() == "role.bootstrap"
            && string.Equals(e.GetProperty("target").GetString(), key.GetPublicAddress(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Jwt_CarriesRoleClaims()
    {
        var (client, _, _) = await TestSupport.SignInAsync(_factory, RbacFactory.AdminKey);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(client.DefaultRequestHeaders.Authorization!.Parameter);

        Assert.Equal(["Admin", "User"], token.Claims.Where(c => c.Type == "role").Select(c => c.Value).Order().ToArray());
    }

    [Fact]
    public async Task RolesInTheToken_AreNotTrusted_ARevokedRoleStopsWorkingImmediately()
    {
        var admin = await AdminAsync();
        var (client, userId, key) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/queue/{evt.EventId}/join", null)).StatusCode);

        // The token still says "User", but the database no longer does.
        var revoke = await admin.DeleteAsync($"/admin/users/{key.GetPublicAddress()}/roles/User");
        revoke.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/queue/{evt.EventId}/join", null)).StatusCode);

        // Signing in again does not hand the role back.
        var (again, _, _) = await TestSupport.SignInAsync(_factory, key);
        Assert.Empty(Roles(await again.GetFromJsonAsync<JsonElement>("/auth/me")));
        Assert.Equal(HttpStatusCode.Forbidden, (await again.PostAsync($"/queue/{evt.EventId}/join", null)).StatusCode);
        _ = userId;
    }

    [Fact]
    public async Task ThereIsNoSelfServiceEscalation()
    {
        var (client, _, key) = await TestSupport.SignInAsync(_factory);

        var grant = await client.PostAsJsonAsync($"/admin/users/{key.GetPublicAddress()}/roles", new { role = "Admin" });

        Assert.Equal(HttpStatusCode.Forbidden, grant.StatusCode);
        Assert.Equal(["User"], Roles(await client.GetFromJsonAsync<JsonElement>("/auth/me")));
    }

    // ---------------------------------------------------------------------
    // 401 / 403 / 200 for every endpoint group
    // ---------------------------------------------------------------------

    public static TheoryData<string, string> ProtectedEndpoints => new()
    {
        { "GET", "/auth/me" },
        { "POST", "/queue/{event}/join" },
        { "GET", "/queue/{event}/position" },
        { "POST", "/checkout/reserve" },
        { "POST", "/checkout/confirm" },
        { "GET", "/tickets" },
        { "GET", "/tickets/{id}" },
        { "GET", "/orders/{id}/voucher" },
        { "POST", "/redeem/{id}/challenge" },
        { "POST", "/redeem/{id}/confirm" },
        { "GET", "/redeem/events" },
        { "GET", "/events/{event}/checkins" },
        { "POST", "/events" },
        { "GET", "/admin/users" },
        { "POST", "/admin/users/0x0000000000000000000000000000000000000001/roles" },
        { "DELETE", "/admin/users/0x0000000000000000000000000000000000000001/roles/Staff" },
        { "GET", "/admin/events/{event}/staff" },
        { "POST", "/admin/events/{event}/cancel" },
        { "GET", "/admin/audit-log" },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithoutToken_Returns401(string method, string path)
    {
        var response = await SendAsync(_factory.CreateClient(), method, path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, string, string> ForbiddenCombinations => new()
    {
        // Staff cannot buy.
        { "Staff", "POST", "/queue/{event}/join" },
        { "Staff", "GET", "/queue/{event}/position" },
        { "Staff", "POST", "/checkout/reserve" },
        { "Staff", "POST", "/checkout/confirm" },
        { "Staff", "GET", "/orders/{id}/voucher" },
        // Buyers cannot scan or see check-in data.
        { "User", "POST", "/redeem/{id}/challenge" },
        { "User", "POST", "/redeem/{id}/confirm" },
        { "User", "GET", "/redeem/events" },
        { "User", "GET", "/events/{event}/checkins" },
        // Only admins manage events, roles and the audit log.
        { "User", "POST", "/events" },
        { "Staff", "POST", "/events" },
        { "User", "GET", "/admin/users" },
        { "Staff", "GET", "/admin/users" },
        { "User", "POST", "/admin/users/0x0000000000000000000000000000000000000001/roles" },
        { "Staff", "DELETE", "/admin/users/0x0000000000000000000000000000000000000001/roles/Staff" },
        { "Staff", "GET", "/admin/events/{event}/staff" },
        { "User", "POST", "/admin/events/{event}/cancel" },
        { "Staff", "POST", "/admin/events/{event}/cancel" },
        { "User", "GET", "/admin/audit-log" },
    };

    [Theory]
    [MemberData(nameof(ForbiddenCombinations))]
    public async Task ProtectedEndpoint_WithTheWrongRole_Returns403(string role, string method, string path)
    {
        var (client, _, _) = await TestSupport.SignInAsAsync(_factory, Enum.Parse<Role>(role));
        var response = await SendAsync(client, method, path);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_CanUseTheBuyerEndpoints()
    {
        var (client, userId, _) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/queue/{evt.EventId}/join", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/queue/{evt.EventId}/position")).StatusCode);

        var orderId = await TestSupport.ReserveOrderAsync(_factory, client, userId, evt, 1);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/checkout/confirm", new { orderId })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/tickets")).StatusCode);
    }

    [Fact]
    public async Task Staff_CanUseTheScannerEndpoints()
    {
        var staff = await TestSupport.StaffClientAsync(_factory);
        var (ticketId, eventId, ownerKey) = await MintedTicketAsync();

        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/redeem/events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/events/{eventId}/checkins")).StatusCode);

        var challenge = await staff.PostAsync($"/redeem/{ticketId}/challenge", null);
        Assert.Equal(HttpStatusCode.OK, challenge.StatusCode);
        var body = await challenge.Content.ReadFromJsonAsync<JsonElement>();
        var signature = new EthereumMessageSigner().EncodeUTF8AndSign(body.GetProperty("challenge").GetString()!, ownerKey);

        var confirm = await staff.PostAsJsonAsync($"/redeem/{ticketId}/confirm",
            new { challenge = body.GetProperty("challenge").GetString(), signature });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
    }

    [Fact]
    public async Task Admin_CanManageEventsButCannotBuyOrScan()
    {
        var admin = await AdminAsync();
        var evt = await TestSupport.SeedEventAsync(_factory);
        var (ticketId, eventId, _) = await MintedTicketAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsync($"/queue/{evt.EventId}/join", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/redeem/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsync($"/redeem/{ticketId}/challenge", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync($"/events/{eventId}/checkins")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/admin/audit-log")).StatusCode);

        var created = await admin.PostAsJsonAsync("/events", new
        {
            name = "Admin Night",
            description = "Created by an admin.",
            venueName = "Hall",
            startsAt = DateTimeOffset.UtcNow.AddDays(5),
            tiers = new[] { new { name = "GA", priceUsdc = 10, totalSupply = 50 } }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task PublicEndpoints_NeedNoToken()
    {
        var client = _factory.CreateClient();
        var evt = await TestSupport.SeedEventAsync(_factory);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/events/{evt.EventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/events/{evt.EventId}/activity")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/chain")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/chain/config")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/auth/nonce")).StatusCode);
    }

    // ---------------------------------------------------------------------
    // Resource-level rules: a role alone is not enough
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Ticket_IsReadableByItsOwnerAndByAdmins_ButNotByOtherUsers()
    {
        var (owner, ownerId, _) = await TestSupport.SignInAsync(_factory);
        var (other, _, _) = await TestSupport.SignInAsync(_factory);
        var admin = await AdminAsync();
        var evt = await TestSupport.SeedEventAsync(_factory);
        var orderId = await TestSupport.ReserveOrderAsync(_factory, owner, ownerId, evt, 1);

        Guid ticketId;
        using (var scope = _factory.Services.CreateScope())
        {
            ticketId = (await scope.ServiceProvider.GetRequiredService<TixFlowDbContext>()
                .Tickets.SingleAsync(t => t.OrderId == orderId)).Id;
        }

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/tickets/{ticketId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/tickets/{ticketId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/tickets/{ticketId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/tickets/{Guid.NewGuid()}")).StatusCode);

        // The list only ever contains the caller's own tickets.
        var othersTickets = await other.GetFromJsonAsync<JsonElement>("/tickets");
        Assert.DoesNotContain(othersTickets.EnumerateArray(), t => t.GetProperty("id").GetGuid() == ticketId);
    }

    [Fact]
    public async Task Staff_CanScanEveryActiveEventRegardlessOfAssignment()
    {
        var admin = await AdminAsync();
        var (staff, _, staffKey) = await TestSupport.SignInAsAsync(_factory, Role.Staff);
        var (assignedTicket, assignedEvent, _) = await MintedTicketAsync();
        var (otherTicket, otherEvent, _) = await MintedTicketAsync();

        // Unassigned staff are not restricted.
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/events/{otherEvent}/checkins")).StatusCode);

        var assign = await admin.PutAsync($"/admin/events/{assignedEvent}/staff/{staffKey.GetPublicAddress()}", null);
        assign.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/redeem/{assignedTicket}/challenge", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/redeem/{otherTicket}/challenge", null)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await staff.PostAsJsonAsync($"/redeem/{otherTicket}/confirm", new { challenge = "x", signature = "0x00" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/events/{otherEvent}/checkins")).StatusCode);

        var visible = await staff.GetFromJsonAsync<JsonElement>("/redeem/events");
        Assert.False(visible.GetProperty("scopedToAssignments").GetBoolean());
        Assert.Contains(assignedEvent, visible.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("id").GetGuid()));
        Assert.Contains(otherEvent, visible.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("id").GetGuid()));

        var me = await staff.GetFromJsonAsync<JsonElement>("/auth/me");
        Assert.Equal([assignedEvent], me.GetProperty("staffEventIds").EnumerateArray().Select(e => e.GetGuid()).ToArray());

        // Removing the assignment does not change the Staff scanning scope.
        (await admin.DeleteAsync($"/admin/events/{assignedEvent}/staff/{staffKey.GetPublicAddress()}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/redeem/{otherTicket}/challenge", null)).StatusCode);
    }

    [Fact]
    public async Task ScannerResponses_ContainNoPersonalData()
    {
        var staff = await TestSupport.StaffClientAsync(_factory);
        var (ticketId, _, ownerKey) = await MintedTicketAsync();

        var challenge = await staff.PostAsync($"/redeem/{ticketId}/challenge", null);
        var challengeText = await challenge.Content.ReadAsStringAsync();
        var challengeBody = JsonSerializer.Deserialize<JsonElement>(challengeText);
        var signature = new EthereumMessageSigner().EncodeUTF8AndSign(challengeBody.GetProperty("challenge").GetString()!, ownerKey);
        var confirmText = await (await staff.PostAsJsonAsync($"/redeem/{ticketId}/confirm",
            new { challenge = challengeBody.GetProperty("challenge").GetString(), signature })).Content.ReadAsStringAsync();

        var owner = ownerKey.GetPublicAddress();
        foreach (var text in new[] { challengeText, confirmText })
        {
            Assert.DoesNotContain(owner, text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("wallet", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownerId", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("eventName", text);
            Assert.Contains("tierName", text);
        }
    }

    // ---------------------------------------------------------------------
    // Role administration
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Admin_CanGrantAndRevokeRoles_AndEveryChangeIsAudited()
    {
        var admin = await AdminAsync();
        var (target, _, targetKey) = await TestSupport.SignInAsync(_factory);
        var wallet = targetKey.GetPublicAddress();

        (await admin.PostAsJsonAsync($"/admin/users/{wallet}/roles", new { role = "Staff" })).EnsureSuccessStatusCode();
        Assert.Equal(["Staff", "User"], Roles(await target.GetFromJsonAsync<JsonElement>("/auth/me")));

        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync($"/admin/users/{wallet}/roles", new { role = "Staff" })).StatusCode);

        (await admin.DeleteAsync($"/admin/users/{wallet}/roles/Staff")).EnsureSuccessStatusCode();
        Assert.Equal(["User"], Roles(await target.GetFromJsonAsync<JsonElement>("/auth/me")));

        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/admin/users/{wallet}/roles/Staff")).StatusCode);

        var audit = (await admin.GetFromJsonAsync<JsonElement>("/admin/audit-log")).EnumerateArray()
            .Where(e => string.Equals(e.GetProperty("target").GetString(), wallet, StringComparison.OrdinalIgnoreCase))
            .Select(e => $"{e.GetProperty("action").GetString()}:{e.GetProperty("details").GetString()}")
            .ToList();
        Assert.Equal(["role.revoke:Staff", "role.grant:Staff"], audit);
        Assert.All((await admin.GetFromJsonAsync<JsonElement>("/admin/audit-log")).EnumerateArray()
                .Where(e => e.GetProperty("action").GetString()!.StartsWith("role.") && e.GetProperty("action").GetString() != "role.bootstrap"),
            e => Assert.Equal(RbacFactory.AdminKey.GetPublicAddress(), e.GetProperty("actorWallet").GetString(), ignoreCase: true));
    }

    [Fact]
    public async Task Admin_CanPrepareRolesForAWalletThatHasNotSignedInYet()
    {
        var admin = await AdminAsync();
        var newStaff = EthECKey.GenerateKey();

        (await admin.PostAsJsonAsync($"/admin/users/{newStaff.GetPublicAddress()}/roles", new { role = "Staff" }))
            .EnsureSuccessStatusCode();

        // The first sign-in keeps the prepared role and does not add the default buyer role on top.
        var (client, _, _) = await TestSupport.SignInAsync(_factory, newStaff);
        Assert.Equal(["Staff"], Roles(await client.GetFromJsonAsync<JsonElement>("/auth/me")));
    }

    [Fact]
    public async Task RoleEndpoints_RejectInvalidInput_AndProtectConfiguredAdmins()
    {
        var admin = await AdminAsync();
        var adminWallet = RbacFactory.AdminKey.GetPublicAddress();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/admin/users/not-a-wallet/roles", new { role = "Staff" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync($"/admin/users/{adminWallet}/roles", new { role = "Superuser" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/admin/users/{adminWallet}/roles/Admin")).StatusCode);

        Assert.Contains("Admin", Roles(await admin.GetFromJsonAsync<JsonElement>("/auth/me")));
    }

    [Fact]
    public async Task AssigningStaff_RequiresTheStaffRole()
    {
        var admin = await AdminAsync();
        var (_, _, buyerKey) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory);

        var response = await admin.PutAsync($"/admin/events/{evt.EventId}/staff/{buyerKey.GetPublicAddress()}", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCancelAnEvent_WhichClosesSalesAndIsAudited()
    {
        var admin = await AdminAsync();
        var (buyer, buyerId, _) = await TestSupport.SignInAsync(_factory);
        var evt = await TestSupport.SeedEventAsync(_factory);

        (await admin.PostAsync($"/admin/events/{evt.EventId}/cancel", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/admin/events/{evt.EventId}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync($"/admin/events/{Guid.NewGuid()}/cancel", null)).StatusCode);

        var detail = await buyer.GetFromJsonAsync<JsonElement>($"/events/{evt.EventId}");
        Assert.NotEqual(JsonValueKind.Null, detail.GetProperty("cancelledAt").ValueKind);

        var reserve = await TestSupport.ReserveAsync(_factory, buyer, buyerId, evt, 1);
        Assert.Equal(HttpStatusCode.Conflict, reserve.StatusCode);

        var audit = await admin.GetFromJsonAsync<JsonElement>("/admin/audit-log");
        Assert.Contains(audit.EnumerateArray(), e =>
            e.GetProperty("action").GetString() == "event.cancel" && e.GetProperty("target").GetString() == evt.EventId.ToString());
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private async Task<HttpClient> AdminAsync() => (await TestSupport.SignInAsync(_factory, RbacFactory.AdminKey)).Client;

    private static string[] Roles(JsonElement me) =>
        me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()!).ToArray();

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string path)
    {
        var url = path.Replace("{event}", Guid.NewGuid().ToString()).Replace("{id}", Guid.NewGuid().ToString());
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });
        return client.SendAsync(request);
    }

    /// <summary>A minted ticket in its own event, owned by a fresh wallet whose key is returned.</summary>
    private async Task<(Guid TicketId, Guid EventId, EthECKey OwnerKey)> MintedTicketAsync()
    {
        var ownerKey = EthECKey.GenerateKey();
        var evt = await TestSupport.SeedEventAsync(_factory);
        var (orderId, ticketIds, _) = await TestSupport.SeedConfirmedOrderAsync(
            _factory, evt, 1, buyerWallet: ownerKey.GetPublicAddress());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TixFlowDbContext>();
        var ticket = await db.Tickets.SingleAsync(t => t.OrderId == orderId);
        ticket.Status = TicketStatus.Minted;
        ticket.TokenId = Random.Shared.Next(1, int.MaxValue);
        await db.SaveChangesAsync();
        return (ticketIds[0], evt.EventId, ownerKey);
    }
}
