using FluentValidation;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Http.Responses;
using MediatR;

namespace EDCL.Shared.Http.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior for FluentValidation.
/// Runs all registered validators before the handler executes.
/// Returns a structured validation failure (never throws exception).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count == 0)
            return await next();

        // Build structured validation errors
        var errors = failures
            .Select(f => new ApiError(
                Field: f.PropertyName,
                Code: f.ErrorCode ?? "Validation.Error",
                Message: f.ErrorMessage))
            .ToList();

        // Try to create a Result<T>.Failure — works when TResponse is Result<T>
        if (TryCreateValidationResult(errors, out var result))
            return result;

        throw new ValidationException(failures);
    }

    private static bool TryCreateValidationResult(
        IEnumerable<ApiError> errors,
        out TResponse result)
    {
        // Handle Result<T> return types
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var errorsList = errors.ToList();
            var errorMessage = string.Join("; ", errorsList.Select(e => e.Message));
            var err = Error.Validation("Validation.Failed", errorMessage);

            var failureMethod = typeof(Result<>)
                .MakeGenericType(typeof(TResponse).GetGenericArguments())
                .GetMethod(nameof(Result<object>.Failure))!;

            result = (TResponse)failureMethod.Invoke(null, [err])!;
            return true;
        }

        result = default!;
        return false;
    }
}
