using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.AuthDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IFirebaseAuthService _firebaseAuthService;

    public AuthController(IAuthService authService, IFirebaseAuthService firebaseAuthService)
    {
        _authService = authService;
        _firebaseAuthService = firebaseAuthService;
    }

    [HttpPost("login")]
    [SwaggerOperation(Summary = "Login", Description = "Login with email and password. Returns accessToken and refreshToken in response data.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<LoginResponseDto>))]
    [SwaggerResponse(400, "Invalid credentials or bad request", typeof(ApiResponse))]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "Login not successful"));
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Data, "Login successful"));
    }

    [HttpPost("request-otp")]
    [SwaggerOperation(Summary = "Request OTP", Description = "Request OTP for register or forgot password. Purpose: \"Register\" or \"ForgotPassword\".")]
    [SwaggerResponse(200, "OTP sent", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequestDto request)
    {
        var result = await _authService.RequestOtpAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "OTP not sent to email"));
        return Ok(ApiResponse.SuccessResponse("OTP sent to email"));
    }

    [HttpPost("verify-otp")]
    [SwaggerOperation(Summary = "Verify OTP", Description = "Verify OTP for register or forgot password. Purpose: \"Register\" or \"ForgotPassword\". For Register, returns LoginResponseDto (auto login).")]
    [SwaggerResponse(200, "OTP verified", typeof(ApiResponse<LoginResponseDto>))]
    [SwaggerResponse(400, "Invalid or expired OTP", typeof(ApiResponse))]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        var result = await _authService.VerifyOtpAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "Invalid or expired OTP"));
        var message = result.Message ?? "OTP verified";
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Data, message));
    }

    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register", Description = "Register with email, password, fullName and verified OTP.")]
    [SwaggerResponse(200, "Registration successful", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad request (e.g. email exists, OTP invalid)", typeof(ApiResponse))]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "Registration not successful"));
        return Ok(ApiResponse.SuccessResponse("Registration successful"));
    }

    [HttpPost("forgot-password")]
    [SwaggerOperation(Summary = "Forgot password (step 1)", Description = "Request OTP with purpose ForgotPassword. Then call reset-password with the OTP.")]
    [SwaggerResponse(200, "OTP sent to email", typeof(ApiResponse))]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "OTP not sent to email"));
        return Ok(ApiResponse.SuccessResponse("OTP sent to email successfully"));
    }

    [HttpPost("reset-password")]
    [SwaggerOperation(Summary = "Reset password (step 2)", Description = "Reset password with email, newPassword and OTP from forgot-password.")]
    [SwaggerResponse(200, "Password reset", typeof(ApiResponse))]
    [SwaggerResponse(400, "Invalid OTP or bad request", typeof(ApiResponse))]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "Password not reset"));
        return Ok(ApiResponse.SuccessResponse("Password reset successfully"));
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
            return Unauthorized(ApiResponse.ErrorResponse("User ID not found in claims"));

        var result = await _authService.ChangePasswordAsync(userId.Value, request);
        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "Password not changed"));
        return Ok(ApiResponse.SuccessResponse("Password changed successfully"));
    }

    [HttpPost("refresh-token")]
    [SwaggerOperation(Summary = "Refresh token", Description = "Get new accessToken and refreshToken using a valid refreshToken.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<LoginResponseDto>))]
    [SwaggerResponse(400, "Invalid or expired refresh token", typeof(ApiResponse))]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "Refresh token not refreshed"));
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Data, "Login successful"));
    }

    [HttpPost("login-google")]
    [SwaggerOperation(Summary = "Login with Google", Description = "FE gửi Google ID token (sau khi đăng nhập Google, lấy credential.idToken hoặc user.getIdToken()). Backend xác thực token, tìm hoặc tạo user với IsEmailVerified = true, trả về accessToken và refreshToken.")]
    [SwaggerResponse(200, "Login successful", typeof(ApiResponse<LoginResponseDto>))]
    [SwaggerResponse(400, "Invalid or expired Google token", typeof(ApiResponse))]
    public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginRequestDto request)
    {
        var googleUser = await _firebaseAuthService.VerifyIdTokenAsync(request.IdToken);
        if (googleUser == null)
            return BadRequest(ApiResponse.ErrorResponse("Invalid or expired Google token"));

        var result = await _authService.LoginWithGoogleAsync(
            googleUser.Email,
            googleUser.DisplayName);

        if (!result.Success)
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "Login not successful"));
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Data, "Login successful"));
    }
}
