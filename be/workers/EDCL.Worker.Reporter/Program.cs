using EDCL.Worker.Reporter.Consumers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    // Add Consumer
    x.AddConsumer<ManifestDeliveredConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Use RabbitMQ from Docker
        cfg.Host("amqp://localhost:5672");
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
