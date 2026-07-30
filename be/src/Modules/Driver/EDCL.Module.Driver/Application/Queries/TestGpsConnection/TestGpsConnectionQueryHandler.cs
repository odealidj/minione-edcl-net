using System.Net.Http.Headers;
using System.Text;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using EDCL.Module.Driver.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Driver.Application.Queries.TestGpsConnection;

public class TestGpsConnectionQueryHandler(IHttpClientFactory httpClientFactory, DriverDbContext dbContext, ILogger<TestGpsConnectionQueryHandler> logger) : IRequestHandler<TestGpsConnectionQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(TestGpsConnectionQuery request, CancellationToken cancellationToken)
    {
        var vendor = await dbContext.GpsVendors.FirstOrDefaultAsync(v => v.Id == request.Id && !v.IsDeleted, cancellationToken);
        if (vendor is null)
        {
            return Result<bool>.Failure(Error.NotFound("GpsVendor.NotFound", "GPS Vendor not found."));
        }

        if (string.IsNullOrWhiteSpace(vendor.ApiUrl))
        {
            vendor.UpdateConnectionStatus("Failed", DateTime.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", "API URL is required to test the connection."));
        }

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        bool isSuccess = false;
        string? errorMessage = null;

        try
        {
            HttpResponseMessage response;

            switch ((int)vendor.ProviderType)
            {
                case 1: // Innovatrack
                    if (string.IsNullOrWhiteSpace(vendor.ApiUsername) || string.IsNullOrWhiteSpace(vendor.ApiPassword))
                    {
                        errorMessage = "Innovatrack requires Username (memberCode) and Password.";
                        break;
                    }
                        
                    var innovatrackUrl = $"{vendor.ApiUrl.TrimEnd('/')}?memberCode={vendor.ApiUsername}&password={vendor.ApiPassword}";
                    response = await client.GetAsync(innovatrackUrl, cancellationToken);
                    isSuccess = response.IsSuccessStatusCode;
                    if (!isSuccess) errorMessage = $"Server responded with status code: {(int)response.StatusCode}";
                    break;

                case 2: // Jitra
                    if (string.IsNullOrWhiteSpace(vendor.ApiToken))
                    {
                        errorMessage = "Jitra requires Token (used as Basic Auth username).";
                        break;
                    }

                    var authString = $"{vendor.ApiToken}:";
                    var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
                    
                    var jitraRequest = new HttpRequestMessage(HttpMethod.Get, vendor.ApiUrl);
                    jitraRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);
                    response = await client.SendAsync(jitraRequest, cancellationToken);
                    isSuccess = response.IsSuccessStatusCode;
                    if (!isSuccess) errorMessage = $"Server responded with status code: {(int)response.StatusCode}";
                    break;

                case 3: // Muliatrack
                    if (string.IsNullOrWhiteSpace(vendor.ApiUsername) || string.IsNullOrWhiteSpace(vendor.ApiPassword))
                    {
                        errorMessage = "Muliatrack requires Username (memberCode) and Password.";
                        break;
                    }
                        
                    var muliatrackUrl = $"{vendor.ApiUrl.TrimEnd('/')}?memberCode={vendor.ApiUsername}&password={vendor.ApiPassword}&lastPositionId=16012315&maxCount=1";
                    response = await client.GetAsync(muliatrackUrl, cancellationToken);
                    isSuccess = response.IsSuccessStatusCode;
                    if (!isSuccess) errorMessage = $"Server responded with status code: {(int)response.StatusCode}";
                    break;

                case 4: // Puninar
                    if (string.IsNullOrWhiteSpace(vendor.ApiToken))
                    {
                        errorMessage = "Puninar requires Token.";
                        break;
                    }

                    var puninarRequest = new HttpRequestMessage(HttpMethod.Post, vendor.ApiUrl);
                    puninarRequest.Headers.Add("token", vendor.ApiToken);
                    response = await client.SendAsync(puninarRequest, cancellationToken);
                    isSuccess = response.IsSuccessStatusCode;
                    if (!isSuccess) errorMessage = $"Server responded with status code: {(int)response.StatusCode}";
                    break;

                default:
                    errorMessage = "Unknown or unsupported GPS Provider Type.";
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to test GPS connection to {ApiUrl}", vendor.ApiUrl);
            errorMessage = $"Connection failed: {ex.Message}";
        }

        vendor.UpdateConnectionStatus(isSuccess ? "Connected" : "Failed", DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (isSuccess)
        {
            return Result<bool>.Success(true);
        }
        else
        {
            return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", errorMessage ?? "Connection failed."));
        }
    }
}
