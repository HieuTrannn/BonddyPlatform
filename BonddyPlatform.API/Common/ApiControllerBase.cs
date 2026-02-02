using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BonddyPlatform.API.Common;

/// <summary>
/// Base controller with standardized response methods
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Returns a successful response with data
    /// </summary>
    protected IActionResult Success<T>(T? data = default, string? message = null)
        => Ok(ApiResponse<T>.SuccessResponse(data, message));

    /// <summary>
    /// Returns a successful response without data
    /// </summary>
    protected IActionResult Success(string? message = null)
        => Ok(ApiResponse.SuccessResponse(message));

    /// <summary>
    /// Returns a 201 Created response with data
    /// </summary>
    protected IActionResult Created<T>(T? data = default, string? message = null)
        => StatusCode(StatusCodes.Status201Created, ApiResponse<T>.SuccessResponse(data, message));

    /// <summary>
    /// Returns a 201 Created response with location header
    /// </summary>
    protected IActionResult CreatedAtAction<T>(string actionName, object routeValues, T? data = default, string? message = null)
        => CreatedAtAction(actionName, routeValues, ApiResponse<T>.SuccessResponse(data, message));

    /// <summary>
    /// Returns a 201 Created response with location header (route name)
    /// </summary>
    protected IActionResult CreatedAtRoute<T>(string routeName, object routeValues, T? data = default, string? message = null)
        => CreatedAtRoute(routeName, routeValues, ApiResponse<T>.SuccessResponse(data, message));

    /// <summary>
    /// Returns a 204 No Content response
    /// </summary>
    protected new IActionResult NoContent()
        => base.NoContent();

    /// <summary>
    /// Returns a 400 Bad Request response
    /// </summary>
    protected IActionResult BadRequest(string message, List<string>? errors = null)
        => base.BadRequest(ApiResponse.ErrorResponse(message, errors));

    /// <summary>
    /// Returns a 400 Bad Request response with single error
    /// </summary>
    protected IActionResult BadRequest(string message, string error)
        => base.BadRequest(ApiResponse.ErrorResponse(message, error));

    /// <summary>
    /// Returns a 400 Bad Request response with data
    /// </summary>
    protected IActionResult BadRequest<T>(string message, T? data = default, List<string>? errors = null)
        => base.BadRequest(ApiResponse<T>.ErrorResponse(message, errors));

    /// <summary>
    /// Returns a 401 Unauthorized response
    /// </summary>
    protected IActionResult Unauthorized(string? message = "Unauthorized")
        => base.Unauthorized(ApiResponse.ErrorResponse(message ?? "Unauthorized"));

    /// <summary>
    /// Returns a 403 Forbidden response
    /// </summary>
    protected IActionResult Forbidden(string? message = "Forbidden")
        => StatusCode(StatusCodes.Status403Forbidden, ApiResponse.ErrorResponse(message ?? "Forbidden"));

    /// <summary>
    /// Returns a 404 Not Found response
    /// </summary>
    protected IActionResult NotFound(string? message = "Resource not found")
        => base.NotFound(ApiResponse.ErrorResponse(message ?? "Resource not found"));

    /// <summary>
    /// Returns a 404 Not Found response with data
    /// </summary>
    protected IActionResult NotFound<T>(string? message = "Resource not found", T? data = default)
        => base.NotFound(ApiResponse<T>.ErrorResponse(message ?? "Resource not found"));

    /// <summary>
    /// Returns a 409 Conflict response
    /// </summary>
    protected IActionResult Conflict(string message)
        => StatusCode(StatusCodes.Status409Conflict, ApiResponse.ErrorResponse(message));

    /// <summary>
    /// Returns a 422 Unprocessable Entity response
    /// </summary>
    protected IActionResult UnprocessableEntity(string message, List<string>? errors = null)
        => StatusCode(StatusCodes.Status422UnprocessableEntity, ApiResponse.ErrorResponse(message, errors));

    /// <summary>
    /// Returns a 500 Internal Server Error response
    /// </summary>
    protected IActionResult InternalServerError(string? message = "An error occurred while processing your request")
        => StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.ErrorResponse(message ?? "An error occurred while processing your request"));

    /// <summary>
    /// Handles exceptions and returns appropriate response
    /// </summary>
    protected IActionResult HandleException(Exception ex)
    {
        return ex switch
        {
            KeyNotFoundException => NotFound(ex.Message),
            InvalidOperationException => BadRequest(ex.Message),
            ArgumentException => BadRequest(ex.Message),
            UnauthorizedAccessException => Unauthorized(ex.Message),
            _ => InternalServerError("An unexpected error occurred")
        };
    }

    /// <summary>
    /// Executes an async operation and handles exceptions
    /// </summary>
    protected async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> operation, Func<T, IActionResult>? onSuccess = null)
    {
        try
        {
            var result = await operation();
            return onSuccess?.Invoke(result) ?? Success(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// Executes an async operation without return value and handles exceptions
    /// </summary>
    protected async Task<IActionResult> ExecuteAsync(Func<Task> operation, IActionResult? onSuccess = null)
    {
        try
        {
            await operation();
            return onSuccess ?? Success();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }

    /// <summary>
    /// Gets the current user ID from claims or throws Unauthorized
    /// </summary>
    protected int GetCurrentUserIdOrThrow()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            throw new UnauthorizedAccessException("User ID not found in claims");
        return userId.Value;
    }

    /// <summary>
    /// Gets the current user email from claims
    /// </summary>
    protected string? GetCurrentUserEmail()
        => User.FindFirst(ClaimTypes.Email)?.Value;

    /// <summary>
    /// Gets a claim value by type
    /// </summary>
    protected string? GetClaimValue(string claimType)
        => User.FindFirst(claimType)?.Value;
}
