using MediatR;

namespace EDCL.Shared.Http.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior for structured logging.
/// Logs request entry, completion, and duration for every command/query.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "EDCL Request: {RequestName} started. {@Request}",
            requestName, request);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            logger.LogInformation(
                "EDCL Request: {RequestName} completed in {ElapsedMs}ms.",
                requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "EDCL Request: {RequestName} failed after {ElapsedMs}ms.",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
