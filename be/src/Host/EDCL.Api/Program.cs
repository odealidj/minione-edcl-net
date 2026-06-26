using EDCL.Module.Auth;
using EDCL.Module.Job;
using EDCL.Module.Cargo;
using EDCL.Module.Notification;
using EDCL.Shared.Http;
using EDCL.Shared.Infrastructure;
using MessagePack.AspNetCoreMvcFormatter;
using Scalar.AspNetCore;
using Serilog;

// ── Bootstrap Serilog (before Host build) ─────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting EDCL API Host...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (full configuration from appsettings) ─────────────────────────
    builder.Host.UseSerilog((ctx, _, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext());

    // ── Graceful Shutdown ─────────────────────────────────────────────────────
    builder.Services.Configure<HostOptions>(o =>
        o.ShutdownTimeout = TimeSpan.FromSeconds(30));

    // ── Shared Infrastructure (Redis Sentinel, EF Interceptors) ──────────────
    builder.Services.AddEdclSharedInfrastructure(builder.Configuration);

    // ── Current User (reads from IHttpContextAccessor) ────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddEdclCurrentUser();

    // ── Shared HTTP Pipeline (MediatR behaviors, middlewares) ─────────────────
    builder.Services.AddEdclSharedHttp();

    // ── Modules ───────────────────────────────────────────────────────────────
    builder.Services.AddEdclModuleAuth(builder.Configuration);
    builder.Services.AddEdclModuleJob(builder.Configuration);
    builder.Services.AddEdclModuleCargo(builder.Configuration);
    builder.Services.AddEdclModuleNotification(builder.Configuration);

    // ── Controllers + Content Negotiation (JSON + MessagePack) ───────────────
    builder.Services.AddControllers(opts =>
    {
        // Input formatters (Accept header negotiation)
        opts.InputFormatters.Add(new MessagePackInputFormatter());
        // Output formatters
        opts.OutputFormatters.Add(new MessagePackOutputFormatter());
    });

    // ── OpenAPI / Scalar / Swagger ────────────────────────────────────────────
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();

    // ── Health Checks ─────────────────────────────────────────────────────────
    var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    var redisConn = builder.Configuration.GetConnectionString("RedisConnection") ?? "";
    var rabbitmqConn = builder.Configuration.GetConnectionString("RabbitMqConnection") ?? "amqp://rabbitmq:5672";

    builder.Services.AddHealthChecks()
        .AddSqlServer(defaultConn, name: "Database", tags: new[] { "db", "sql", "sqlserver" })
        .AddRedis(redisConn, name: "Redis", tags: new[] { "cache", "redis" });
        //.AddRabbitMQ(setup => setup.ConnectionUri = new Uri(rabbitmqConn), name: "RabbitMQ", tags: new[] { "messagebroker", "rabbitmq" });

    var app = builder.Build();

    // ── Apply Migrations on Startup ───────────────────────────────────────────
    await app.Services.ApplyAuthMigrationsAsync();
    await app.Services.ApplyJobMigrationsAsync();
    await app.Services.ApplyCargoMigrationsAsync();
    await app.Services.ApplyNotificationMigrationsAsync();

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("TraceId", ctx.Items["TraceId"]);
            diag.Set("UserId", ctx.User.FindFirst("sub")?.Value);
        };
    });

    app.UseMiddleware<EDCL.Shared.Http.Middlewares.TraceIdMiddleware>();
    app.UseMiddleware<EDCL.Shared.Http.Middlewares.GlobalExceptionHandlerMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Swagger UI & Scalar UI (dev only or configurable)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/openapi/v1.json", "EDCL Mini API v1");
        });

        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.Title  = "EDCL Mini API";
            opts.Theme  = Scalar.AspNetCore.ScalarTheme.DeepSpace;
        });
    }

    await app.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "EDCL API Host terminated unexpectedly.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
