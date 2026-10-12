using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TixFlow.Api.Admin;
using TixFlow.Api.Auth;
using TixFlow.Api.Checkout;
using TixFlow.Api.Demo;
using TixFlow.Api.Queue;
using TixFlow.Api.Redeem;
using TixFlow.Api.Events;
using TixFlow.Api.Health;
using TixFlow.Api.Chain;
using TixFlow.Api.Tickets;
using TixFlow.Infrastructure.Chain;
using TixFlow.Infrastructure.Data;
using TixFlow.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<TixFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

// Auth services
builder.Services.AddSingleton<NonceStore>();
builder.Services.AddScoped<SiweService>();
builder.Services.AddScoped<RoleService>();

// Queue services
builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection(QueueOptions.SectionName));
builder.Services.AddScoped<IQueueStore, QueueService>();
builder.Services.AddScoped<AdmissionTokenService>();

// Redeem challenge store
builder.Services.AddScoped<IChallengeStore, RedisChallengeStore>();

// Chain access (voucher signing, RPC with failover, smart-wallet signature checks)
builder.Services.AddTixFlowChain(builder.Configuration);
var chainEnabled = builder.Configuration.GetValue<bool>("Chain:Enabled");

// Background workers must run in exactly one process: the relayer account has one nonce sequence.
// A single `dotnet run` does everything; in Docker the api container sets HostedWorkers:Enabled=false
// and a dedicated workers container sets it to true.
if (builder.Configuration.GetValue("HostedWorkers:Enabled", true))
{
    builder.Services.AddHostedService<AdmissionWorker>();
    if (chainEnabled)
        builder.Services.AddTixFlowChainWorkers(builder.Configuration);
    else if (builder.Configuration.GetValue<bool>("Demo:SimulateMint"))
        builder.Services.AddHostedService<DemoMintWorker>();
}

// CORS for frontend dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

// JWT auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            RoleClaimType = Policies.RoleClaim
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
            // Roles are loaded from the database on every request, never trusted from the token, so a
            // revoked role stops working immediately. A token whose user no longer exists (e.g. after a
            // database reset) is rejected so the client signs in again.
            OnTokenValidated = async context =>
            {
                var sub = context.Principal?.FindFirst("sub")?.Value;
                var db = context.HttpContext.RequestServices.GetRequiredService<TixFlowDbContext>();
                var account = Guid.TryParse(sub, out var userId)
                    ? await db.Users
                        .Where(u => u.Id == userId)
                        .Select(u => new { Roles = u.Roles.Select(r => r.Role).ToList() })
                        .FirstOrDefaultAsync()
                    : null;

                if (account is null || context.Principal?.Identity is not ClaimsIdentity identity)
                {
                    context.Fail("User no longer exists.");
                    return;
                }

                foreach (var stale in identity.FindAll(Policies.RoleClaim).ToList())
                    identity.RemoveClaim(stale);
                foreach (var role in account.Roles)
                    identity.AddClaim(new Claim(Policies.RoleClaim, role.ToString()));
            }
        };
    });

builder.Services.AddAuthorization(options => options.AddTixFlowPolicies());

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("queue-join", httpContext =>
    {
        var userId = httpContext.User.FindFirst("sub")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(userId, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(10),
            SegmentsPerWindow = 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

var app = builder.Build();

// One-shot mode for the migrator container: migrate, seed, and exit without serving requests.
if (args.Contains("--migrate-and-seed"))
{
    await app.InitializeDatabaseAsync(force: true);
    return;
}

await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapAuthEndpoints();
app.MapEventEndpoints();
app.MapQueueEndpoints();
app.MapCheckoutEndpoints();
app.MapTicketEndpoints();
app.MapRedeemEndpoints();
app.MapHealthEndpoints();
app.MapChainEndpoints();
app.MapAdminEndpoints();
app.MapDemoEndpoints();
app.MapHub<QueueHub>("/hubs/queue");

app.Run();

public partial class Program { }
