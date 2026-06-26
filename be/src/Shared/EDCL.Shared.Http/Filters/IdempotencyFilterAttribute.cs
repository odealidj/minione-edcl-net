using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Ports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace EDCL.Shared.Http.Filters;

/// <summary>
/// A filter that ensures an operation is executed only once per X-Idempotency-Key and DriverId.
/// </summary>
public sealed class IdempotencyFilterAttribute : IAsyncActionFilter
{
    private const string IdempotencyHeader = "X-Idempotency-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 1. Get the Idempotency Key from the header
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyHeader, out var idempotencyKeyValues))
        {
            var traceId = context.HttpContext.TraceIdentifier;
            context.Result = new BadRequestObjectResult(
                ApiResponse<object>.Fail($"Missing {IdempotencyHeader} header.", traceId, 400));
            return;
        }

        var idempotencyKey = idempotencyKeyValues.ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var traceId = context.HttpContext.TraceIdentifier;
            context.Result = new BadRequestObjectResult(
                ApiResponse<object>.Fail($"Empty {IdempotencyHeader} header.", traceId, 400));
            return;
        }

        // 2. Resolve required services
        var cachePort = context.HttpContext.RequestServices.GetRequiredService<ICachePort>();
        var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        var driverId = currentUserService.DriverId ?? 0;
        if (driverId == 0)
        {
            // Fallback for endpoints where DriverId isn't found, though it shouldn't happen here.
            var traceId = currentUserService.CurrentTraceId ?? context.HttpContext.TraceIdentifier;
            context.Result = new UnauthorizedObjectResult(
                ApiResponse<object>.Fail("Unauthorized.", traceId, 401));
            return;
        }

        var cacheKey = CacheKeys.Idempotency(driverId, idempotencyKey);

        // 3. Check if key already exists
        var exists = await cachePort.ExistsAsync(cacheKey, context.HttpContext.RequestAborted);
        if (exists)
        {
            var traceId = currentUserService.CurrentTraceId ?? context.HttpContext.TraceIdentifier;
            context.Result = new ConflictObjectResult(
                ApiResponse<object>.Fail("Duplicate request detected. Action already processed.", traceId, 409));
            return;
        }

        // 4. Register key to prevent future double clicks
        // We set it before execution. If execution fails internally due to a transient error, 
        // ideally we would allow retry, but for simplicity of preventing double taps, setting it eagerly is safest.
        await cachePort.SetAsync(cacheKey, true, CacheTtl.Idempotency, context.HttpContext.RequestAborted);

        // 5. Continue execution
        await next();
    }
}
