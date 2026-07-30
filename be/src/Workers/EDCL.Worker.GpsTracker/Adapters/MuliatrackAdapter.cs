using System.Text.Json;
using System.Text.Json.Serialization;
using EDCL.Module.Driver.Domain.Entities;
using EDCL.Worker.GpsTracker.Models;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Adapters;

public sealed class MuliatrackAdapter(HttpClient httpClient, ILogger<MuliatrackAdapter> logger) : IGpsVendorAdapter
{
    public async Task<List<NormalizedGpsPoint>> GetLatestLocationsAsync(GpsVendor config, CancellationToken cancellationToken)
    {
        var result = new List<NormalizedGpsPoint>();

        if (string.IsNullOrWhiteSpace(config.ApiUrl))
        {
            logger.LogWarning("Muliatrack configuration is incomplete for GpsVendor {Code}", config.Code);
            return result;
        }

        try
        {
            // For demo purposes, if URL has placeholder, replace with 0. 
            // In a real scenario, we should fetch the max PositionId from DB.
            var url = config.ApiUrl.Replace("{lastPositionId}", "0");
            
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<List<MuliatrackResponseItem>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data != null)
            {
                foreach (var item in data)
                {
                    double.TryParse(item.X, out var lng);
                    double.TryParse(item.Y, out var lat);
                    double.TryParse(item.Speed, out var speed);
                    double.TryParse(item.Course, out var course);
                    DateTime.TryParse(item.DateTime, out var timestamp);
                    bool.TryParse(item.Engine, out var engine);

                    result.Add(new NormalizedGpsPoint
                    {
                        GpsVehicleId = item.VehicleId ?? item.VehicleNumber ?? string.Empty,
                        Latitude = lat,
                        Longitude = lng,
                        Speed = speed,
                        Heading = course,
                        Timestamp = timestamp,
                        IsEngineOn = engine,
                        ProviderName = "Muliatrack"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching data from Muliatrack for GpsVendor {Code}", config.Code);
        }

        return result;
    }

    private class MuliatrackResponseItem
    {
        [JsonPropertyName("PositionId")]
        public string? PositionId { get; set; }
        
        [JsonPropertyName("VehicleId")]
        public string? VehicleId { get; set; }
        
        [JsonPropertyName("VehicleNumber")]
        public string? VehicleNumber { get; set; }
        
        [JsonPropertyName("DateTime")]
        public string? DateTime { get; set; }
        
        [JsonPropertyName("X")]
        public string? X { get; set; }
        
        [JsonPropertyName("Y")]
        public string? Y { get; set; }
        
        [JsonPropertyName("Speed")]
        public string? Speed { get; set; }
        
        [JsonPropertyName("Course")]
        public string? Course { get; set; }
        
        [JsonPropertyName("Engine")]
        public string? Engine { get; set; }
    }
}
