using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Worker.GpsTracker;
using EDCL.Worker.GpsTracker.Adapters;
using EDCL.Worker.GpsTracker.Consumers;
using EDCL.Worker.GpsTracker.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using EDCL.Shared.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Ports;

var builder = Host.CreateApplicationBuilder(args);

// Configure DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost,1433;Database=EDCL;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";
builder.Services.AddDbContext<DriverDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDbContext<EDCL.Module.Job.Infrastructure.Persistence.JobDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Shared Infrastructure
builder.Services.AddScoped<ICurrentUserService, SystemUserService>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

// Register HttpClients and Adapters
builder.Services.AddHttpClient<InnovatrackAdapter>();
builder.Services.AddHttpClient<JitraAdapter>();
builder.Services.AddHttpClient<MuliatrackAdapter>();
builder.Services.AddHttpClient<PuninarAdapter>();

builder.Services.AddHttpClient<OsrmClient>();

builder.Services.AddSingleton<IGpsAdapterFactory, GpsAdapterFactory>();
builder.Services.AddSingleton<SimulationSessionManager>();
builder.Services.AddTransient<GeofenceService>();

// Configure MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SimulationStartedConsumer>();
    x.AddConsumer<JobStartedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq") ?? "amqp://guest:guest@localhost:5672");
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddHostedService<RealGpsPollingWorker>();
builder.Services.AddHostedService<RouteSimulatorWorker>();

var host = builder.Build();
host.Run();
