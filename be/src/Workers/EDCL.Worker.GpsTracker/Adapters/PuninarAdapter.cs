using System.Text.Json;
using System.Text.Json.Serialization;
using EDCL.Module.Driver.Domain.Entities;
using EDCL.Worker.GpsTracker.Models;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Adapters;

public sealed class PuninarAdapter(HttpClient httpClient, ILogger<PuninarAdapter> logger) : IGpsVendorAdapter
{
    public async Task<List<NormalizedGpsPoint>> GetLatestLocationsAsync(GpsVendor config, CancellationToken cancellationToken)
    {
        var result = new List<NormalizedGpsPoint>();

        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ApiToken))
        {
            logger.LogWarning("Puninar configuration is incomplete for GpsVendor {Code}", config.Code);
            return result;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, config.ApiUrl);
            request.Headers.Add("token", config.ApiToken);

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize<PuninarResponseRoot>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed?.Data != null)
            {
                foreach (var item in parsed.Data)
                {
                    DateTime.TryParse(item.GpsTime, out var timestamp);

                    result.Add(new NormalizedGpsPoint
                    {
                        GpsVehicleId = item.Nopol ?? string.Empty,
                        Latitude = item.Latitude,
                        Longitude = item.Longitude,
                        Speed = item.Velocity,
                        Heading = double.TryParse(item.Direction, out var dir) ? dir : 0,
                        Timestamp = timestamp,
                        IsEngineOn = item.Engine == 1,
                        ProviderName = "Puninar"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching data from Puninar for GpsVendor {Code}", config.Code);
        }

        return result;
    }

    private class PuninarResponseRoot
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("data")]
        public List<PuninarResponseItem>? Data { get; set; }
    }

    private class PuninarResponseItem
    {
        [JsonPropertyName("nopol")]
        public string? Nopol { get; set; }
        
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
        
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        
        [JsonPropertyName("gps_time")]
        public string? GpsTime { get; set; }
        
        [JsonPropertyName("velocity")]
        public double Velocity { get; set; }
        
        [JsonPropertyName("direction")]
        public string? Direction { get; set; }
        
        [JsonPropertyName("engine")]
        public int Engine { get; set; }
    }
}
