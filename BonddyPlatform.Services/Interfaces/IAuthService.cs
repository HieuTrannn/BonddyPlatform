using BonddyPlatform.Services.DTOs.AuthDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Message, LoginResponseDto? Data)> LoginAsync(LoginRequestDto request);
    Task<(bool Success, string? Message, LoginResponseDto? Data)> LoginWithFirebaseAsync(string email, string? fullName, string? redirectPath);
    Task<(bool Success, string? Message)> RegisterAsync(RegisterRequestDto request);
    Task<(bool Success, string? Message)> RequestOtpAsync(RequestOtpRequestDto request);
    Task<(bool Success, string? Message)> VerifyOtpAsync(VerifyOtpRequestDto request);
    Task<(bool Success, string? Message)> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<(bool Success, string? Message)> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<(bool Success, string? Message)> ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
    Task<(bool Success, string? Message, LoginResponseDto? Data)> RefreshTokenAsync(RefreshTokenRequestDto request);
}
