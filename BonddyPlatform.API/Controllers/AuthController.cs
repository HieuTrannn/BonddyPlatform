using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.AuthDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [SwaggerOperation(Summary = "Login", Description = "Login with email and password. Returns accessToken and refreshToken in response data.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<LoginResponseDto>))]
    [SwaggerResponse(400, "Invalid credentials or bad request", typeof(ApiResponse))]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var (success, message, data) = await _authService.LoginAsync(request);
        if (!success)
            return BadRequest(message);
        return Success(data, message);
    }

    [HttpPost("request-otp")]
    [SwaggerOperation(Summary = "Request OTP", Description = "Request OTP for register or forgot password. Purpose: \"Register\" or \"ForgotPassword\".")]
    [SwaggerResponse(200, "OTP sent", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequestDto request)
    {
        var (success, message) = await _authService.RequestOtpAsync(request);
        if (!success)
            return BadRequest(message);
        return Success(message);
    }

    [HttpPost("verify-otp")]
    [SwaggerOperation(Summary = "Verify OTP", Description = "Verify OTP for register or forgot password.")]
    [SwaggerResponse(200, "OTP verified", typeof(ApiResponse))]
    [SwaggerResponse(400, "Invalid or expired OTP", typeof(ApiResponse))]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        var (success, message) = await _authService.VerifyOtpAsync(request);
        if (!success)
            return BadRequest(message);
        return Success(message);
    }

    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register", Description = "Register with email, password, fullName and verified OTP.")]
    [SwaggerResponse(200, "Registration successful", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad request (e.g. email exists, OTP invalid)", typeof(ApiResponse))]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var (success, message) = await _authService.RegisterAsync(request);
        if (!success)
            return BadRequest(message);
        return Success(message);
    }

    [HttpPost("forgot-password")]
    [SwaggerOperation(Summary = "Forgot password (step 1)", Description = "Request OTP with purpose ForgotPassword. Then call reset-password with the OTP.")]
    [SwaggerResponse(200, "OTP sent to email", typeof(ApiResponse))]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var (success, message) = await _authService.ForgotPasswordAsync(request);
        return Success(message);
    }

    [HttpPost("reset-password")]
    [SwaggerOperation(Summary = "Reset password (step 2)", Description = "Reset password with email, newPassword and OTP from forgot-password.")]
    [SwaggerResponse(200, "Password reset", typeof(ApiResponse))]
    [SwaggerResponse(400, "Invalid OTP or bad request", typeof(ApiResponse))]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var (success, message) = await _authService.ResetPasswordAsync(request);
        if (!success)
            return BadRequest(message);
        return Success(message);
    }

    [HttpPost("change-password")]
    [Authorize]
    [SwaggerOperation(Summary = "Change password", Description = "Change password for the authenticated user. Requires Bearer token.")]
    [SwaggerResponse(200, "Password changed", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad request (e.g. wrong current password)", typeof(ApiResponse))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("User ID not found in claims");

        var (success, message) = await _authService.ChangePasswordAsync(userId.Value, request);
        if (!success)
            return BadRequest(message);
        return Success(message);
    }

    [HttpPost("refresh-token")]
    [SwaggerOperation(Summary = "Refresh token", Description = "Get new accessToken and refreshToken using a valid refreshToken.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<LoginResponseDto>))]
    [SwaggerResponse(400, "Invalid or expired refresh token", typeof(ApiResponse))]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var (success, message, data) = await _authService.RefreshTokenAsync(request);
        if (!success)
            return BadRequest(message);
        return Success(data, message);
    }
}
