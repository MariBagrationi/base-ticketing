using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Signer;
using TixFlow.Api.Auth;

namespace TixFlow.Tests;

public class AuthTests : IClassFixture<TixFlowWebFactory>
{
    private readonly HttpClient _client;
    private readonly TixFlowWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AuthTests(TixFlowWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetNonce_ReturnsNonce()
    {
        var response = await _client.GetAsync("/auth/nonce");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<NonceResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Nonce));
    }

    [Fact]
    public async Task FullAuthFlow_Succeeds()
    {
        var nonce = await GetNonce();
        var (message, signature) = SignSiweMessage(nonce);

        var verifyResponse = await _client.PostAsJsonAsync("/auth/verify",
            new { message, signature });
        verifyResponse.EnsureSuccessStatusCode();

        var tokenBody = await verifyResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOpts);
        Assert.NotNull(tokenBody);
        Assert.False(string.IsNullOrWhiteSpace(tokenBody.Token));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenBody.Token);
        var meResponse = await _client.GetAsync("/auth/me");
        meResponse.EnsureSuccessStatusCode();

        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>(JsonOpts);
        Assert.NotNull(me);
        Assert.NotNull(me.Wallet);
        Assert.StartsWith("0x", me.Wallet);
    }

    [Fact]
    public async Task NonceReuse_Fails()
    {
        var nonce = await GetNonce();
        var (message, signature) = SignSiweMessage(nonce);

        var first = await _client.PostAsJsonAsync("/auth/verify",
            new { message, signature });
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/auth/verify",
            new { message, signature });
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    [Fact]
    public async Task InvalidSignature_Fails()
    {
        var nonce = await GetNonce();
        var (message, _) = SignSiweMessage(nonce);

        var badSig = "0x" + new string('0', 130);
        var response = await _client.PostAsJsonAsync("/auth/verify",
            new { message, signature = badSig });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> GetNonce()
    {
        var response = await _client.GetFromJsonAsync<NonceResponse>("/auth/nonce", JsonOpts);
        return response!.Nonce;
    }

    private static (string message, string signature) SignSiweMessage(string nonce)
    {
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

        return (message, signature);
    }

    private record NonceResponse(string Nonce);
    private record TokenResponse(string Token);
    private record MeResponse(string? UserId, string? Wallet);
}
