using System.Net.Http.Headers;
using System.Text;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Driver.Application.Queries.TestGpsConnection;

public class TestGpsConnectionQueryHandler(IHttpClientFactory httpClientFactory, ILogger<TestGpsConnectionQueryHandler> logger) : IRequestHandler<TestGpsConnectionQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(TestGpsConnectionQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ApiUrl))
        {
            return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", "API URL is required to test the connection."));
        }

        var client = httpClientFactory.CreateClient();
        // Ensure a reasonable timeout for testing
        client.Timeout = TimeSpan.FromSeconds(10);

        try
        {
            HttpResponseMessage response;

            switch (request.ProviderType)
            {
                case 1: // Innovatrack
                    if (string.IsNullOrWhiteSpace(request.ApiUsername) || string.IsNullOrWhiteSpace(request.ApiPassword))
                        return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", "Innovatrack requires Username (memberCode) and Password."));
                        
                    var innovatrackUrl = $"{request.ApiUrl.TrimEnd('/')}?memberCode={request.ApiUsername}&password={request.ApiPassword}";
                    response = await client.GetAsync(innovatrackUrl, cancellationToken);
                    break;

                case 2: // Jitra
                    if (string.IsNullOrWhiteSpace(request.ApiUsername))
                        return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", "Jitra requires Username for Basic Auth."));

                    var jitraUrl = request.ApiUrl;
                    var authString = $"{request.ApiUsername}:";
                    var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
                    
                    var jitraRequest = new HttpRequestMessage(HttpMethod.Get, jitraUrl);
                    jitraRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);
                    response = await client.SendAsync(jitraRequest, cancellationToken);
                    break;

                case 3: // Muliatrack
                    if (string.IsNullOrWhiteSpace(request.ApiUsername) || string.IsNullOrWhiteSpace(request.ApiPassword))
                        return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", "Muliatrack requires Username (memberCode) and Password."));
                        
                    var muliatrackUrl = $"{request.ApiUrl.TrimEnd('/')}?memberCode={request.ApiUsername}&password={request.ApiPassword}&lastPositionId=16012315&maxCount=1";
                    response = await client.GetAsync(muliatrackUrl, cancellationToken);
                    break;

                case 4: // Puninar
                    if (string.IsNullOrWhiteSpace(request.ApiToken))
                        return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", "Puninar requires Token."));

                    var puninarUrl = request.ApiUrl;
                    var puninarRequest = new HttpRequestMessage(HttpMethod.Post, puninarUrl);
                    puninarRequest.Headers.Add("token", request.ApiToken);
                    response = await client.SendAsync(puninarRequest, cancellationToken);
                    break;

                default:
                    return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", "Unknown or unsupported GPS Provider Type."));
            }

            if (response.IsSuccessStatusCode)
            {
                return Result<bool>.Success(true);
            }
            else
            {
                return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", $"Connection failed. Server responded with status code: {(int)response.StatusCode} ({response.ReasonPhrase})."));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to test GPS connection to {ApiUrl}", request.ApiUrl);
            return Result<bool>.Failure(new Error("GpsVendor.ConnectionFailed", $"Connection failed: {ex.Message}"));
        }
    }
}
