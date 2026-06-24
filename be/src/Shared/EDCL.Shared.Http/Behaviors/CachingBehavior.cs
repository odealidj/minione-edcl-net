using EDCL.Shared.Kernel.Ports;
using MediatR;

namespace EDCL.Shared.Http.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior for Query-level caching.
/// Only applies to requests implementing ICachedQuery&lt;TResponse&gt;.
/// Commands are never cached — only reads.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(ICachePort cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICachedQuery<TResponse> cachedQuery)
            return await next();

        var cached = await cache.GetAsync<TResponse>(cachedQuery.CacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var response = await next();

        if (response is not null)
            await cache.SetAsync(cachedQuery.CacheKey, response, cachedQuery.Ttl, cancellationToken);

        return response;
    }
}

/// <summary>
/// Implement this on a Query to enable automatic response caching via CachingBehavior.
/// </summary>
public interface ICachedQuery<out TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    TimeSpan Ttl { get; }
}
