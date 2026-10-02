namespace TixFlow.Api.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/health").WithTags("Health");

        group.MapGet("/chain", (IConfiguration config) =>
        {
            var hasRpc = !string.IsNullOrWhiteSpace(config["Rpc:BaseUrl"]);
            var simulated = config.GetValue<bool>("Demo:SimulateMint");

            if (hasRpc)
                return Results.Ok(new { status = "Healthy", rpcConfigured = true, message = "Connected to Base." });

            if (simulated)
                return Results.Ok(new
                {
                    status = "Simulated",
                    rpcConfigured = false,
                    message = "Demo mode: payments and NFT minting are simulated locally."
                });

            return Results.Ok(new
            {
                status = "Degraded",
                rpcConfigured = false,
                message = "Chain unavailable. Purchases are saved and NFTs will mint once the connection is restored."
            });
        });
    }
}
