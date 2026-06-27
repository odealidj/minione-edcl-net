using EDCL.Module.Cargo.Infrastructure.Channels;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Cargo.Api;

[ApiController]
[Route("api/v1/cargo/ingestion")]
public class IngestionController : ControllerBase
{
    private readonly IngestionErrorChannel _errorChannel;

    public IngestionController(IngestionErrorChannel errorChannel)
    {
        _errorChannel = errorChannel;
    }

    [HttpGet("errors/stream")]
    public async Task GetErrorsStream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

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
}
