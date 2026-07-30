using System.Text.Json;
using System.Text.Json.Serialization;
using EDCL.Module.Driver.Domain.Entities;
using EDCL.Worker.GpsTracker.Models;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Adapters;

public sealed class InnovatrackAdapter(HttpClient httpClient, ILogger<InnovatrackAdapter> logger) : IGpsVendorAdapter
{
    public async Task<List<NormalizedGpsPoint>> GetLatestLocationsAsync(GpsVendor config, CancellationToken cancellationToken)
    {
        var result = new List<NormalizedGpsPoint>();
        
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ApiUsername) || string.IsNullOrWhiteSpace(config.ApiPassword))
        {
            logger.LogWarning("Innovatrack configuration is incomplete for GpsVendor {Code}", config.Code);
            return result;
        }

        // e.g. https://api.inovatrack.com/api/VehicleSummary/GetAll
        var url = $"{config.ApiUrl.TrimEnd('/')}?memberCode={config.ApiUsername}&password={config.ApiPassword}";

        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<List<InnovatrackResponseItem>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data != null)
            {
                foreach (var item in data)
                {
                    result.Add(new NormalizedGpsPoint
                    {
                        GpsVehicleId = item.AvlUnitId ?? item.VehicleNumber ?? item.VehicleId.ToString(),
                        Latitude = item.Lat,
                        Longitude = item.Lon,
                        Speed = item.Speed,
                        Heading = item.Course,
                        Timestamp = item.DateTime,
                        IsEngineOn = item.Engine,
                        ProviderName = "Innovatrack"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching data from Innovatrack for GpsVendor {Code}", config.Code);
        }

        return result;
    }

    private class InnovatrackResponseItem
    {
        [JsonPropertyName("vehicle_id")]
        public long VehicleId { get; set; }
        
        [JsonPropertyName("vehicle_number")]
        public string? VehicleNumber { get; set; }
        
        [JsonPropertyName("avl_unit_id")]
        public string? AvlUnitId { get; set; }
        
        [JsonPropertyName("date_time")]
        public DateTime DateTime { get; set; }
        
        [JsonPropertyName("lon")]
        public double Lon { get; set; }
        
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        
        [JsonPropertyName("speed")]
        public double Speed { get; set; }
        
        [JsonPropertyName("course")]
        public double Course { get; set; }
        
        [JsonPropertyName("engine")]
        public bool Engine { get; set; }
    }
}
