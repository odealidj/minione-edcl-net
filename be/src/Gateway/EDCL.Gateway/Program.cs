using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, _, config) =>
    config.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext());

// ── YARP Reverse Proxy ────────────────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── JWT Auth (validated at Gateway before forwarding) ────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opts =>
    {
        opts.TokenValidationParameters = new()
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey  = true,
            ValidIssuer              = jwtSection["Issuer"],
            ValidAudience            = jwtSection["Audience"],
            IssuerSigningKey         = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSection["AccessTokenSecret"]!)),
            ClockSkew                = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ── Rate Limiting (built-in .NET 7+) ─────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(ctx =>
    {
        var driverId = ctx.User.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(driverId, _ =>
            new() { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) });
    });
    opts.RejectionStatusCode = 429;
});

// ── Graceful Shutdown ─────────────────────────────────────────────────────────
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();

// Inject Trace ID from/to all proxied requests
app.Use(async (ctx, next) =>
{
    var traceId = ctx.Request.Headers["X-Trace-Id"].FirstOrDefault()
               ?? System.Diagnostics.Activity.Current?.Id
               ?? Guid.NewGuid().ToString("N")[..32];
    ctx.Items["TraceId"] = traceId;
    ctx.Response.Headers["X-Trace-Id"] = traceId;
    // Forward to upstream
    ctx.Request.Headers["X-Trace-Id"] = traceId;
    await next();
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

await app.RunAsync();
