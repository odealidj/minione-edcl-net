namespace EDCL.Shared.Kernel.Common;

/// <summary>
/// Represents a structured error with a code for client-side handling
/// and a human-readable message for display.
/// </summary>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type = ErrorType.Failure)
{
    // ── Common reusable errors ──────────────────────────────────────────
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.", ErrorType.Failure);

    // ── Factory helpers ─────────────────────────────────────────────────
    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} with id '{id}' was not found.", ErrorType.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error Unauthorized(string code = "Auth.Unauthorized", string message = "Authentication is required.") =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code = "Auth.Forbidden", string message = "You do not have permission to perform this action.") =>
        new(code, message, ErrorType.Forbidden);

    public static Error BusinessRule(string code, string message) =>
        new(code, message, ErrorType.BusinessRule);
}

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    BusinessRule = 6
}
