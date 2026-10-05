using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TixFlow.Api.Auth;
using TixFlow.Api.Checkout;
using TixFlow.Api.Demo;
using TixFlow.Api.Queue;
using TixFlow.Api.Redeem;
using TixFlow.Api.Events;
using TixFlow.Api.Health;
using TixFlow.Api.Tickets;
using TixFlow.Infrastructure.Data;

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

// Queue services
builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection(QueueOptions.SectionName));
builder.Services.AddScoped<IQueueStore, QueueService>();
builder.Services.AddScoped<AdmissionTokenService>();
builder.Services.AddHostedService<AdmissionWorker>();

// Redeem challenge store
builder.Services.AddScoped<IChallengeStore, RedisChallengeStore>();

if (builder.Configuration.GetValue<bool>("Demo:SimulateMint"))
    builder.Services.AddHostedService<DemoMintWorker>();

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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
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
            // A token can outlive its user (e.g. after a database reset); reject it so the client signs in again.
            OnTokenValidated = async context =>
            {
                var sub = context.Principal?.FindFirst("sub")?.Value;
                var db = context.HttpContext.RequestServices.GetRequiredService<TixFlowDbContext>();
                if (!Guid.TryParse(sub, out var userId) || !await db.Users.AnyAsync(u => u.Id == userId))
                    context.Fail("User no longer exists.");
            }
        };
    });

builder.Services.AddAuthorization();

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
app.MapHub<QueueHub>("/hubs/queue");

app.Run();

public partial class Program { }
