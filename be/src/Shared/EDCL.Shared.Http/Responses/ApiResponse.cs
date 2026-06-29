using System.Text.Json.Serialization;

namespace EDCL.Shared.Http.Responses;

/// <summary>
/// Unified API response envelope used across ALL endpoints.
/// Ensures consistent structure for success, error, and paginated responses.
/// </summary>
public sealed class ApiResponse<T>
{
    [JsonPropertyName("trace_id")]
    public string TraceId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;       // "success" | "error"

    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("pagination")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMeta? Pagination { get; init; }

    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ApiError>? Errors { get; init; }

    // ── Factory Methods ──────────────────────────────────────────────────

    public static ApiResponse<T> Success(T data, string traceId, int code = 200, string message = "OK")
        => new()
        {
            TraceId = traceId,
            Status = "success",
            Code = code,
            Message = message,
            Data = data
        };

    public static ApiResponse<T> Created(T data, string traceId, string message = "Resource created successfully.")
        => new()
        {
            TraceId = traceId,
            Status = "success",
            Code = 201,
            Message = message,
            Data = data
        };

    public static ApiResponse<T> Paginated(T data, PaginationMeta pagination, string traceId)
        => new()
        {
            TraceId = traceId,
            Status = "success",
            Code = 200,
            Message = "OK",
            Data = data,
            Pagination = pagination
        };

    public static ApiResponse<T> Fail(
        string message,
        string traceId,
        int code,
        IEnumerable<ApiError>? errors = null)
        => new()
        {
            TraceId = traceId,
            Status = "error",
            Code = code,
            Message = message,
            Errors = errors
        };
}

/// <summary>Pagination metadata returned with list responses.</summary>
public sealed class PaginationMeta
{
    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; init; }

    [JsonPropertyName("total_items")]
    public long TotalItems { get; init; }

    [JsonPropertyName("total_pages")]
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

    [JsonPropertyName("has_next")]
    public bool HasNext => Page < TotalPages;

    [JsonPropertyName("has_previous")]
    public bool HasPrevious => Page > 1;

    [JsonPropertyName("nextPage")]
    public int? NextPage => HasNext ? Page + 1 : null;

    [JsonPropertyName("prevPage")]
    public int? PrevPage => HasPrevious ? Page - 1 : null;

    public static PaginationMeta From(int page, int pageSize, long totalItems)
        => new() { Page = page, PageSize = pageSize, TotalItems = totalItems };
}

/// <summary>Individual field-level error detail.</summary>
public sealed record ApiError(
    [property: JsonPropertyName("field")]   string Field,
    [property: JsonPropertyName("code")]    string Code,
    [property: JsonPropertyName("message")] string Message);
