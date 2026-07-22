using EDCL.Module.Cargo.Infrastructure.Channels;
using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Http.Responses;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace EDCL.Module.Cargo.Api;

[ApiController]
[Route("api/v1/web/cargo/ingestion")]
public class IngestionController : ControllerBase
{
    private readonly IngestionErrorChannel _errorChannel;
    private readonly IngestionMetricsChannel _metricsChannel;

    public IngestionController(IngestionErrorChannel errorChannel, IngestionMetricsChannel metricsChannel)
    {
        _errorChannel = errorChannel;
        _metricsChannel = metricsChannel;
    }

    [HttpGet("errors/stream")]
    public async Task GetErrorsStream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        // Force headers (including CORS) to be sent immediately
        await Response.WriteAsync(":\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        var errorsAsyncStream = _errorChannel.ReadAllAsync(cancellationToken);

        await foreach (var error in errorsAsyncStream)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var errorJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                error.Id,
                error.EventType,
                error.ErrorMessage,
                error.OccurredAt,
                error.Payload
            });

            await Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpGet("metrics/stream")]
    public async Task GetMetricsStream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        // Force headers (including CORS) to be sent immediately
        await Response.WriteAsync(":\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        var tcs = new TaskCompletionSource();
        cancellationToken.Register(() => tcs.TrySetResult());

        Action<EDCL.Module.Cargo.Domain.Events.IngestionMetricsEvent> onMetricsReceived = async (metrics) =>
        {
            var metricsJson = System.Text.Json.JsonSerializer.Serialize(metrics);
            try
            {
                await Response.WriteAsync($"data: {metricsJson}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch
            {
                // Client disconnected
            }
        };

        _metricsChannel.OnMetricsReceived += onMetricsReceived;

        try
        {
            await tcs.Task;
        }
        finally
        {
            _metricsChannel.OnMetricsReceived -= onMetricsReceived;
        }
    }

    [HttpGet("sessions")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetSessions([FromServices] CargoDbContext dbContext, CancellationToken cancellationToken)
    {
        var sessions = await dbContext.SyncSessions
            .OrderByDescending(x => x.SessionDate)
            .ThenByDescending(x => x.StartTime)
            .Take(20)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Success(sessions, HttpContext.TraceIdentifier));
    }

    [HttpGet("errors")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetErrors([FromServices] CargoDbContext dbContext, CancellationToken cancellationToken)
    {
        var errors = await dbContext.IngestionErrors
            .OrderByDescending(x => x.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Success(errors, HttpContext.TraceIdentifier));
    }

    [HttpPost("errors/retry")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RetryErrors([FromBody] RetryErrorsRequest request, [FromServices] CargoDbContext dbContext, [FromServices] ISendEndpointProvider sendEndpointProvider, CancellationToken cancellationToken)
    {
        if (request.ErrorIds == null || request.ErrorIds.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("No Error IDs provided", HttpContext.TraceIdentifier, 400));
        }

        var errors = await dbContext.IngestionErrors
            .Where(x => request.ErrorIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        int retryCount = 0;
        foreach (var error in errors)
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:edcl_ingestion"));
            await endpoint.Send(JsonDocument.Parse(error.Payload).RootElement, cancellationToken);
            
            retryCount++;
            dbContext.IngestionErrors.Remove(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Success(new { Retried = retryCount }, HttpContext.TraceIdentifier));
    }
}

public class RetryErrorsRequest
{
    public List<long> ErrorIds { get; set; } = new();
}
