using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using EDCL.Module.Driver.Domain.Entities;
using EDCL.Worker.GpsTracker.Models;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Adapters;

public sealed class JitraAdapter(HttpClient httpClient, ILogger<JitraAdapter> logger) : IGpsVendorAdapter
{
    public async Task<List<NormalizedGpsPoint>> GetLatestLocationsAsync(GpsVendor config, CancellationToken cancellationToken)
    {
        var result = new List<NormalizedGpsPoint>();

        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ApiUsername))
        {
            logger.LogWarning("Jitra configuration is incomplete for GpsVendor {Code}", config.Code);
            return result;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, config.ApiUrl);
            
            // JITRA uses Basic Auth where username is the token/key
            var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{config.ApiUsername}:{config.ApiPassword ?? string.Empty}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<List<JitraResponseItem>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data != null)
            {
                foreach (var item in data)
                {
                    double.TryParse(item.Lat, out var lat);
                    double.TryParse(item.Lng, out var lng);
                    double.TryParse(item.Speed, out var speed);
                    double.TryParse(item.Angle, out var angle);
                    DateTime.TryParse(item.Datetime, out var timestamp);

                    result.Add(new NormalizedGpsPoint
                    {
                        GpsVehicleId = item.Id ?? string.Empty,
                        Latitude = lat,
                        Longitude = lng,
                        Speed = speed,
                        Heading = angle,
                        Timestamp = timestamp,
                        IsEngineOn = speed > 0, // Fallback since no engine flag in JITRA sample
                        ProviderName = "Jitra"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching data from Jitra for GpsVendor {Code}", config.Code);
        }

        return result;
    }

    private class JitraResponseItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("datetime")]
        public string? Datetime { get; set; }
        
        [JsonPropertyName("lat")]
        public string? Lat { get; set; }
        
        [JsonPropertyName("lng")]
        public string? Lng { get; set; }
        
        [JsonPropertyName("angle")]
        public string? Angle { get; set; }
        
        [JsonPropertyName("speed")]
        public string? Speed { get; set; }
    }
}
