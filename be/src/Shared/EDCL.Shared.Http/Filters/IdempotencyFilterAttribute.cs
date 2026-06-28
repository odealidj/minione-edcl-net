using System.Text.Json;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Ports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace EDCL.Shared.Http.Filters;

public sealed class CachedResponse
{
    public int StatusCode { get; set; }
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// A filter that ensures an operation is executed only once per X-Idempotency-Key and DriverId.
/// </summary>
public sealed class IdempotencyFilterAttribute : IAsyncActionFilter
{
    private const string IdempotencyHeader = "X-Idempotency-Key";
    private const string InProgressMarker = "IN_PROGRESS";

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
        var cachedResponse = await cachePort.GetAsync<CachedResponse>(cacheKey, context.HttpContext.RequestAborted);
        if (cachedResponse != null)
        {
            if (cachedResponse.Body == InProgressMarker)
            {
                var traceId = currentUserService.CurrentTraceId ?? context.HttpContext.TraceIdentifier;
                context.Result = new ConflictObjectResult(
                    ApiResponse<object>.Fail("Duplicate request detected. Action is currently being processed.", traceId, 409));
                return;
            }

            // Return the cached successful response
            context.Result = new ContentResult
            {
                StatusCode = cachedResponse.StatusCode,
                Content = cachedResponse.Body,
                ContentType = "application/json"
            };
            return;
        }

        // 4. Register key as IN_PROGRESS to prevent concurrent double clicks
        var inProgressResponse = new CachedResponse { StatusCode = 202, Body = InProgressMarker };
        await cachePort.SetAsync(cacheKey, inProgressResponse, CacheTtl.Idempotency, context.HttpContext.RequestAborted);

        // 5. Continue execution
        var executedContext = await next();

        // 6. If execution was successful, cache the actual response
        if (executedContext.Exception == null && executedContext.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? 200;
            if (statusCode >= 200 && statusCode < 300)
            {
                // Must explicitly specify PropertyNamingPolicy because by default it might use PascalCase,
                // but we usually return camelCase JSON in ASP.NET Core. 
                // ASP.NET Core defaults to camelCase.
                var jsonBody = JsonSerializer.Serialize(objectResult.Value, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var successResponse = new CachedResponse { StatusCode = statusCode, Body = jsonBody };
                await cachePort.SetAsync(cacheKey, successResponse, CacheTtl.Idempotency, context.HttpContext.RequestAborted);
            }
            else
            {
                // If the operation failed (e.g. 400 Bad Request), we remove the IN_PROGRESS marker
                // so the user can retry.
                await cachePort.RemoveAsync(cacheKey, context.HttpContext.RequestAborted);
            }
        }
        else if (executedContext.Exception != null)
        {
             await cachePort.RemoveAsync(cacheKey, context.HttpContext.RequestAborted);
        }
    }
}
