using System.Text.Json.Serialization;
using EDCL.Shared.Kernel.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EDCL.Api.Consumers;

public class GpsLastPositionHDto
{
    [JsonPropertyName("Id")]
    public Guid Id { get; set; }

    [JsonPropertyName("GpsVendorId")]
    public Guid GpsVendorId { get; set; }

    [JsonPropertyName("VendorName")]
    public string VendorName { get; set; } = string.Empty;

    [JsonPropertyName("CreatedAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("LastModified")]
    public DateTime? LastModified { get; set; }

    [JsonPropertyName("Data")]
    public List<GpsLastPositionDDto> Data { get; set; } = new();
}

public class GpsLastPositionDDto
{
    [JsonPropertyName("Id")]
    public Guid Id { get; set; }

    [JsonPropertyName("GpsLastPositionHId")]
    public Guid GpsLastPositionHId { get; set; }

    [JsonPropertyName("Lpcd")]
    public string? Lpcd { get; set; }

    [JsonPropertyName("PlatNo")]
    public string? PlatNo { get; set; }

    [JsonPropertyName("DeviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("Datetime")]
    public DateTime Datetime { get; set; }

    [JsonPropertyName("X")]
    public decimal? X { get; set; } // Longitude

    [JsonPropertyName("Y")]
    public decimal? Y { get; set; } // Latitude

    [JsonPropertyName("Speed")]
    public decimal? Speed { get; set; }

    [JsonPropertyName("Course")]
    public decimal? Course { get; set; }

    [JsonPropertyName("StreetName")]
    public string? StreetName { get; set; }
}

/// <summary>
/// Consumes raw GPS telemetry events published by EDCLGPSAPI microservice via RabbitMQ topic_exchange.
/// Saves real-time coordinates to Redis Geospatial index and broadcasts to SignalR TrackingHub.
/// Keeps SQL Server free from millions of noisy raw GPS records.
/// </summary>
public class GpsTelemetryConsumer(
    IConnectionMultiplexer redis,
    IPublishEndpoint publishEndpoint,
    ILogger<GpsTelemetryConsumer> logger) : IConsumer<GpsLastPositionHDto>
{
    public async Task Consume(ConsumeContext<GpsLastPositionHDto> context)
    {
        var message = context.Message;
        if (message.Data == null || message.Data.Count == 0)
        {
            return;
        }

        var db = redis.GetDatabase();

        foreach (var item in message.Data)
        {
            if (string.IsNullOrWhiteSpace(item.PlatNo) || !item.X.HasValue || !item.Y.HasValue)
            {
                continue;
            }

            var lon = (double)item.X.Value;
            var lat = (double)item.Y.Value;
            var platNo = item.PlatNo.Trim();

            // 1. Update Redis Geospatial Index (O(log(N)) spatial query)
            await db.GeoAddAsync("trucks:locations", lon, lat, platNo);

            // 2. Update Live Truck Hash Status in Redis with TTL 24 Hours
            var hashKey = $"truck:{platNo}:telemetry";
            await db.HashSetAsync(hashKey, new HashEntry[]
            {
                new("lat", lat),
                new("lon", lon),
                new("speed", (double)(item.Speed ?? 0)),
                new("course", (double)(item.Course ?? 0)),
                new("street_name", item.StreetName ?? string.Empty),
                new("vendor", message.VendorName ?? string.Empty),
                new("device_id", item.DeviceId ?? string.Empty),
                new("timestamp", item.Datetime.ToString("o"))
            });
            await db.KeyExpireAsync(hashKey, TimeSpan.FromHours(24));

            // 3. Broadcast to Internal EDCL Tracking Event (SignalR live map)
            await publishEndpoint.Publish(new TruckLocationUpdatedIntegrationEvent
            {
                TruckId = 0,
                Latitude = lat,
                Longitude = lon,
                Speed = (double?)item.Speed,
                Heading = (double?)item.Course,
                Timestamp = item.Datetime,
                ProviderName = message.VendorName ?? "Vendor-GPS"
            });

            logger.LogInformation(
                "[GPS Tracking] Ingested position for Truck {PlatNo} ({Lat}, {Lon}) via {Vendor}",
                platNo, lat, lon, message.VendorName);
        }
    }
}
