namespace TixFlow.Api.Demo;

/// <summary>A ready-made wallet the demo UI can import with one click.</summary>
public class DemoAccount
{
    public string Label { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}

public static class DemoEndpoints
{
    public static void MapDemoEndpoints(this WebApplication app)
    {
        // LOCAL DEMOS ONLY. Hands the browser the private keys of the pre-seeded Admin and Staff accounts so
        // a judge can try each role without any wallet setup. The keys configured for the local stack are
        // Anvil's publicly known development keys. Off unless Demo:ExposeAccounts is true.
        app.MapGet("/demo/accounts", (IConfiguration config) =>
        {
            if (!config.GetValue<bool>("Demo:ExposeAccounts"))
                return Results.Ok(Array.Empty<DemoAccount>());

            var accounts = config.GetSection("Demo:Accounts").Get<List<DemoAccount>>() ?? [];
            return Results.Ok(accounts.Where(a => !string.IsNullOrWhiteSpace(a.PrivateKey)));
        }).WithTags("Demo");
    }
}
