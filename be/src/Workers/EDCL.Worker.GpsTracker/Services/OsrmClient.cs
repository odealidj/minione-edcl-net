using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Services;

public sealed class OsrmClient(HttpClient httpClient, ILogger<OsrmClient> logger)
{
    private const string BaseUrl = "http://router.project-osrm.org/route/v1/driving";

    public async Task<List<double[]>?> GetRoutePolylineAsync(double startLat, double startLon, double endLat, double endLon, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{BaseUrl}/{startLon},{startLat};{endLon},{endLat}?overview=full&geometries=geojson";
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OsrmRouteResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Routes != null && result.Routes.Any())
            {
                var coordinates = result.Routes.First().Geometry?.Coordinates;
                return coordinates;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching route from OSRM from {StartLat},{StartLon} to {EndLat},{EndLon}", startLat, startLon, endLat, endLon);
        }

        return null;
    }
}

public class OsrmRouteResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("routes")]
    public List<OsrmRoute>? Routes { get; set; }
}

public class OsrmRoute
{
    [JsonPropertyName("geometry")]
    public OsrmGeometry? Geometry { get; set; }
}

public class OsrmGeometry
{
    [JsonPropertyName("coordinates")]
    public List<double[]>? Coordinates { get; set; }
}
