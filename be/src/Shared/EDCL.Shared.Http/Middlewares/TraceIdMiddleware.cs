using System.Diagnostics;

namespace EDCL.Shared.Http.Middlewares;

/// <summary>
/// Injects a unique trace ID into every request.
/// Priority: W3C TraceParent → X-Trace-Id header → Activity.Current.Id → new GUID.
/// The resolved trace ID is propagated in:
///   - HttpContext.Items["TraceId"]
///   - Response header: X-Trace-Id
///   - Serilog LogContext (appears in every log line of this request)
/// </summary>
public sealed class TraceIdMiddleware : IMiddleware
{
    public const string TraceIdKey = "TraceId";
    public const string TraceIdHeader = "X-Trace-Id";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Resolve trace ID with priority chain
        var traceId = context.Request.Headers[TraceIdHeader].FirstOrDefault()
                   ?? Activity.Current?.Id
                   ?? Guid.NewGuid().ToString("N")[..32];

        context.Items[TraceIdKey] = traceId;
        context.Response.Headers[TraceIdHeader] = traceId;

        // Push to Serilog enricher scope so trace_id appears in all logs
        using (Serilog.Context.LogContext.PushProperty(TraceIdKey, traceId))
        {
            await next(context);
        }
    }
}

/// <summary>
/// Extension to conveniently retrieve the trace ID from HttpContext.
/// </summary>
public static class HttpContextTraceExtensions
{
    public static string GetTraceId(this HttpContext context)
        => context.Items.TryGetValue(TraceIdMiddleware.TraceIdKey, out var traceId)
            ? traceId?.ToString() ?? string.Empty
            : string.Empty;
}
