using EDCL.Module.Auth;
using EDCL.Module.Job;
using EDCL.Module.Driver;
using EDCL.Module.Cargo;
using EDCL.Module.Notification;
using EDCL.Shared.Http;
using EDCL.Shared.Infrastructure;
using MessagePack.AspNetCoreMvcFormatter;
using MassTransit;
using Hangfire;
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

    // ── Forwarded Headers (for YARP Gateway) ──────────────────────────────────
    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // ── Shared Infrastructure (Redis Sentinel, EF Interceptors) ──────────────
    builder.Services.AddEdclSharedInfrastructure(builder.Configuration);

    // ── Current User (reads from IHttpContextAccessor) ────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddEdclCurrentUser();

    // ── Shared HTTP Pipeline (MediatR behaviors, middlewares) ─────────────────
    builder.Services.AddEdclSharedHttp();

    // ── Modules ───────────────────────────────────────────────────────────────
    builder.Services.AddEdclModuleAuth(builder.Configuration);
    builder.Services.AddEdclModuleDriver(builder.Configuration);
    builder.Services.AddEdclModuleJob(builder.Configuration);
    builder.Services.AddEdclModuleCargo(builder.Configuration);
    builder.Services.AddEdclModuleNotification(builder.Configuration);

    // ── Controllers + Content Negotiation (JSON + MessagePack) ───────────────
    builder.Services.AddControllers(opts =>
    {
        // Input formatters (Accept header negotiation)
        opts.InputFormatters.Add(new MessagePackInputFormatter());
        opts.OutputFormatters.Add(new MessagePackOutputFormatter());
    });
    
    // ── SignalR ───────────────────────────────────────────────────────────────
    builder.Services.AddSignalR();

    // ── Messaging / MassTransit (RabbitMQ) ────────────────────────────────────
    builder.Services.AddMassTransit(x =>
    {
        // Register Consumers from Modules
        x.AddConsumers(typeof(EDCL.Module.Cargo.CargoModuleRegistration).Assembly);
        x.AddConsumers(typeof(EDCL.Module.Notification.NotificationModuleRegistration).Assembly);
        x.AddConsumer<EDCL.Api.Consumers.TrackingEventConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            var rabbitMqConn = builder.Configuration.GetConnectionString("RabbitMqConnection") ?? "amqp://localhost:5672";
            cfg.Host(rabbitMqConn);
            
            // Allow MassTransit to consume raw JSON from Debezium natively (even without content-type)
            // (Removed global raw json, only enabled on the specific endpoint)

            cfg.UseMessageRetry(r =>
            {
                // Exponential backoff retry for Foreign Key and other transient SQL issues
                r.Handle<Microsoft.Data.SqlClient.SqlException>();
                r.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
            });

            // Note: Ingestion from Debezium is handled by EDCL.Worker.Ingestion
            
            // Listen for DLQ faults from the ingestion worker
            cfg.ReceiveEndpoint("edcl_ingestion_faults", e =>
            {
                e.ClearSerialization();
                e.UseRawJsonSerializer();
                e.ConfigureConsumer<EDCL.Module.Cargo.Infrastructure.Consumers.IngestionFaultConsumer>(context);
            });

            // Auto configure endpoints for any other consumers not manually configured above
            cfg.ConfigureEndpoints(context);
        });
    });

    // ── CORS ────────────────────────────────────────────────────────────────
    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy("AllowAll", b => b
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
    });

    // ── OpenAPI / Scalar / Swagger ────────────────────────────────────────────
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<EDCL.Api.Infrastructure.OpenApi.BearerSecuritySchemeTransformer>();
    });
    builder.Services.AddEndpointsApiExplorer();

    // ── Hangfire (Scheduled Jobs) ─────────────────────────────────────────────
    var hangfireConn = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(hangfireConn));

    // Server is removed from API, background jobs should only be executed by Workers.

    // ── Health Checks ─────────────────────────────────────────────────────────
    var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    var redisConn = builder.Configuration.GetConnectionString("RedisConnection") ?? "";
    var rabbitmqConn = builder.Configuration.GetConnectionString("RabbitMqConnection") ?? "amqp://rabbitmq:5672";

    builder.Services.AddHealthChecks()
        .AddSqlServer(defaultConn, name: "Database", tags: new[] { "db", "sql", "sqlserver" })
        .AddRedis(redisConn, name: "Redis", tags: new[] { "cache", "redis" });

    var app = builder.Build();

    // ── Apply Migrations on Startup ───────────────────────────────────────────
    await app.Services.ApplyAuthMigrationsAsync();
    await app.Services.ApplyDriverMigrationsAsync();
    await app.Services.ApplyJobMigrationsAsync();
    await app.Services.ApplyCargoMigrationsAsync();
    await app.Services.ApplyNotificationMigrationsAsync();

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("TraceId", ctx.Items["TraceId"] ?? "unknown");
            diag.Set("UserId", ctx.User.FindFirst("sub")?.Value ?? "anonymous");
        };
    });

    app.UseMiddleware<EDCL.Shared.Http.Middlewares.TraceIdMiddleware>();
    app.UseMiddleware<EDCL.Shared.Http.Middlewares.GlobalExceptionHandlerMiddleware>();

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<EDCL.Api.Hubs.TrackingHub>("/hubs/tracking");
    app.UseHangfireDashboard("/hangfire");
    
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
            opts.Authentication = new ScalarAuthenticationOptions
            {
                PreferredSecuritySchemes = new[] { "Bearer" }
            };
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

public partial class Program { }
