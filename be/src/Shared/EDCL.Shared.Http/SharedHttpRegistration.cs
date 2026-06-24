using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Http.Middlewares;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EDCL.Shared.Http;

public static class SharedHttpRegistration
{
    /// <summary>
    /// Registers shared HTTP pipeline services:
    /// MediatR behaviors (in correct pipeline order), middlewares, FluentValidation.
    /// </summary>
    public static IServiceCollection AddEdclSharedHttp(this IServiceCollection services)
    {
        // ── Middlewares ───────────────────────────────────────────────────
        services.AddTransient<TraceIdMiddleware>();
        services.AddTransient<GlobalExceptionHandlerMiddleware>();

        // ── MediatR Pipeline Behaviors (order matters!) ───────────────────
        // Execution order: Logging → Validation → Idempotency → Caching → Handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        return services;
    }
}
