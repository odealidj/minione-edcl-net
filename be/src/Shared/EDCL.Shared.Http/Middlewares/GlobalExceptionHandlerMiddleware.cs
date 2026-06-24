using EDCL.Shared.Http.Responses;
using EDCL.Shared.Http.Middlewares;
using EDCL.Shared.Kernel.Common;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace EDCL.Shared.Http.Middlewares;

/// <summary>
/// Catches all unhandled exceptions and returns a structured ApiResponse error.
/// Ensures no raw exception data leaks to clients in production.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var traceId = context.GetTraceId();
            _logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);
            await HandleExceptionAsync(context, ex, traceId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex, string traceId)
    {
        var (statusCode, message) = ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication required."),
            KeyNotFoundException         => (StatusCodes.Status404NotFound,     ex.Message),
            InvalidOperationException    => (StatusCodes.Status422UnprocessableEntity, ex.Message),
            ArgumentException            => (StatusCodes.Status400BadRequest,   ex.Message),
            _                            => (StatusCodes.Status500InternalServerError,
                                              _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.")
        };

        var response = ApiResponse<object>.Fail(message, traceId, statusCode);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            }));
    }
}
