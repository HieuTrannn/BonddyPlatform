namespace BonddyPlatform.API.Common;

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiResponse()
    {
    }

    public ApiResponse(bool success, string? message = null, T? data = default, List<string>? errors = null)
    {
        Success = success;
        Message = message;
        Data = data;
        Errors = errors;
    }

    public static ApiResponse<T> SuccessResponse(T? data = default, string? message = null)
        => new(true, message, data);

    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        => new(false, message, default, errors);

    public static ApiResponse<T> ErrorResponse(string message, string error)
        => new(false, message, default, new List<string> { error });
}

/// <summary>
/// Non-generic API response for responses without data
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiResponse()
    {
    }

    public ApiResponse(bool success, string? message = null, List<string>? errors = null)
    {
        Success = success;
        Message = message;
        Errors = errors;
    }

    public static ApiResponse SuccessResponse(string? message = null)
        => new(true, message);

    public static ApiResponse ErrorResponse(string message, List<string>? errors = null)
        => new(false, message, errors);

    public static ApiResponse ErrorResponse(string message, string error)
        => new(false, message, new List<string> { error });
}
