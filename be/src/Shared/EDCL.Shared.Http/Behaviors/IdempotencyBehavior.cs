using EDCL.Shared.Kernel.Ports;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace EDCL.Shared.Http.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior for Idempotency.
/// Intercepts Commands (not Queries) with an X-Idempotency-Key header.
/// If the key was already processed → returns the cached response immediately.
/// If not → processes normally and stores the result in Redis for 24 hours.
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>(
    IHttpContextAccessor httpContextAccessor,
    ICachePort cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string IdempotencyHeader = "X-Idempotency-Key";

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply to write operations (Commands), skip Queries
        if (request is IQuery<TResponse>)
            return await next();

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return await next();

        var idempotencyKey = httpContext.Request.Headers[IdempotencyHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return await next();

        // Try to extract driver ID from JWT for namespacing the key
        var driverId = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst("driver_id")?.Value
                    ?? "anon";

        var cacheKey = CacheKeys.Idempotency(long.TryParse(driverId, out var id) ? id : 0, idempotencyKey);

        // Check cache
        var cached = await cache.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            EDCL.Shared.Infrastructure.Telemetry.EdclTelemetry.IdempotencyReplayedCounter
                .Add(1, new KeyValuePair<string, object?>("request_name", typeof(TRequest).Name));
            return cached;
        }

        // Process the request
        var response = await next();

        // Cache the response
        await cache.SetAsync(cacheKey, response, CacheTtl.Idempotency, cancellationToken);

        return response;
    }
}

/// <summary>Marker interface to distinguish Queries from Commands in pipeline.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }

/// <summary>Marker interface for Commands.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }
