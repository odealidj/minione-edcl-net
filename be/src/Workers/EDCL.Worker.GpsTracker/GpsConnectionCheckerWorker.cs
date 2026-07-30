using EDCL.Module.Driver.Domain.Entities;
using EDCL.Module.Driver.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace EDCL.Worker.GpsTracker;

public sealed class GpsConnectionCheckerWorker(
    IServiceProvider serviceProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<GpsConnectionCheckerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GpsConnectionCheckerWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckConnectionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during GPS connection checks.");
            }

            // Sleep for a random interval between 5, 10, 15, or 25 minutes
            var intervals = new[] { 5, 10, 15, 25 };
            var waitMins = intervals[Random.Shared.Next(intervals.Length)];
            logger.LogInformation("GpsConnectionCheckerWorker sleeping for {WaitMins} minutes.", waitMins);
            
            await Task.Delay(TimeSpan.FromMinutes(waitMins), stoppingToken);
        }
    }

    private async Task CheckConnectionsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DriverDbContext>();

        var vendors = await dbContext.GpsVendors.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
        if (!vendors.Any()) return;

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        foreach (var vendor in vendors)
        {
            if (string.IsNullOrWhiteSpace(vendor.ApiUrl))
            {
                vendor.UpdateConnectionStatus("Failed", DateTime.UtcNow);
                continue;
            }

            bool isSuccess = false;

            try
            {
                HttpResponseMessage response;

                switch ((int)vendor.ProviderType)
                {
                    case 1: // Innovatrack
                        if (string.IsNullOrWhiteSpace(vendor.ApiUsername) || string.IsNullOrWhiteSpace(vendor.ApiPassword))
                            break;

                        var innovatrackUrl = $"{vendor.ApiUrl.TrimEnd('/')}?memberCode={vendor.ApiUsername}&password={vendor.ApiPassword}";
                        response = await client.GetAsync(innovatrackUrl, cancellationToken);
                        isSuccess = response.IsSuccessStatusCode;
                        break;

                    case 2: // Jitra
                        if (string.IsNullOrWhiteSpace(vendor.ApiToken))
                            break;

                        var authString = $"{vendor.ApiToken}:";
                        var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
                        var jitraRequest = new HttpRequestMessage(HttpMethod.Get, vendor.ApiUrl);
                        jitraRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);
                        
                        response = await client.SendAsync(jitraRequest, cancellationToken);
                        isSuccess = response.IsSuccessStatusCode;
                        break;

                    case 3: // Muliatrack
                        if (string.IsNullOrWhiteSpace(vendor.ApiUsername) || string.IsNullOrWhiteSpace(vendor.ApiPassword))
                            break;

                        var muliatrackUrl = $"{vendor.ApiUrl.TrimEnd('/')}?memberCode={vendor.ApiUsername}&password={vendor.ApiPassword}&lastPositionId=16012315&maxCount=1";
                        response = await client.GetAsync(muliatrackUrl, cancellationToken);
                        isSuccess = response.IsSuccessStatusCode;
                        break;

                    case 4: // Puninar
                        if (string.IsNullOrWhiteSpace(vendor.ApiToken))
                            break;

                        var puninarRequest = new HttpRequestMessage(HttpMethod.Post, vendor.ApiUrl);
                        puninarRequest.Headers.Add("token", vendor.ApiToken);
                        
                        response = await client.SendAsync(puninarRequest, cancellationToken);
                        isSuccess = response.IsSuccessStatusCode;
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to test connection for vendor {VendorName}", vendor.Name);
            }

            vendor.UpdateConnectionStatus(isSuccess ? "Connected" : "Failed", DateTime.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
