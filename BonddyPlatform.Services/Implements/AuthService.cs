using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Services.DTOs.AuthDtos;
using BonddyPlatform.Services.Interfaces;

namespace BonddyPlatform.Services.Implements;

public class AuthService : IAuthService
{
    private const int OtpExpiryMinutes = 10;
    private const int RefreshTokenExpiryDays = 7;

    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailSender _emailSender;

    public AuthService(IUnitOfWork uow, IJwtTokenGenerator jwtTokenGenerator, IEmailSender emailSender)
    {
        _uow = uow;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailSender = emailSender;
    }

    public async Task<(bool Success, string? Message, LoginResponseDto? Data)> LoginAsync(LoginRequestDto request)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email);
        if (user == null)
            return (false, "Email or password is incorrect", null);

        if (!user.IsEmailVerified)
            return (false, "Please verify your email first. Check your inbox for the OTP.", null);

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return (false, "Email or password is incorrect", null);

        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshTokenValue();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays)
        };
        await _uow.RefreshTokens.AddAsync(refreshToken);
        await _uow.SaveChangesAsync();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var response = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresInSeconds = _jwtTokenGenerator.AccessTokenExpirySeconds,
            TokenType = "Bearer"
        };
        return (true, null, response);
    }

    public async Task<(bool Success, string? Message, LoginResponseDto? Data)> LoginWithGoogleAsync(string email, string? fullName)
    {
        var user = await _uow.Users.GetByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                FullName = fullName?.Trim() ?? email.Split('@')[0],
                Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                IsEmailVerified = true,
                Role = Role.User,
                Gender = Gender.Other
            };
            await _uow.Users.AddAsync(user);
        }
        else
        {
            user.IsEmailVerified = true;
            if (!string.IsNullOrWhiteSpace(fullName))
                user.FullName = fullName.Trim();
            user.UpdatedAt = DateTime.UtcNow;
            _uow.Users.Update(user);
        }

        await _uow.SaveChangesAsync();

        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshTokenValue();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays)
        };
        await _uow.RefreshTokens.AddAsync(refreshToken);
        await _uow.SaveChangesAsync();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var response = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresInSeconds = _jwtTokenGenerator.AccessTokenExpirySeconds,
            TokenType = "Bearer",
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        };
        return (true, "Login successful", response);
    }

    public async Task<(bool Success, string? Message)> RequestOtpAsync(RequestOtpRequestDto request)
    {
        var purpose = ParseOtpPurpose(request.Purpose);
        if (purpose == null)
            return (false, "Invalid purpose. Use Register or ForgotPassword");

        if (purpose == OtpPurpose.Register)
        {
            var existingUser = await _uow.Users.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.IsEmailVerified)
                return (false, "Email already registered");
        }
        else
        {
            var user = await _uow.Users.GetByEmailAsync(request.Email);
            if (user == null)
                return (false, "Email not found");
        }

        var otpCode = GenerateOtpCode();
        var otp = new OtpVerification
        {
            Email = request.Email,
            OtpCode = otpCode,
            Purpose = purpose.Value,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes)
        };
        await _uow.OtpVerifications.AddAsync(otp);
        await _uow.SaveChangesAsync();

        var subject = purpose == OtpPurpose.Register
            ? "Verify your email - Bonddy Platform"
            : "Reset your password - Bonddy Platform";
        var body = $"<p>Your verification code is: <strong>{otpCode}</strong></p><p>Valid for {OtpExpiryMinutes} minutes.</p>";
        await _emailSender.SendEmailAsync(request.Email, subject, body);

        return (true, "OTP has been sent to your email.");
    }

    public async Task<(bool Success, string? Message)> VerifyOtpAsync(VerifyOtpRequestDto request)
    {
        var purpose = ParseOtpPurpose(request.Purpose);
        if (purpose == null)
            return (false, "Invalid purpose");

        var otp = await _uow.OtpVerifications.GetLatestValidAsync(request.Email, purpose.Value);
        if (otp == null)
            return (false, "Invalid or expired OTP");

        if (otp.OtpCode != request.OtpCode.Trim())
            return (false, "Invalid OTP code");

        otp.IsUsed = true;
        _uow.OtpVerifications.Update(otp);

        if (purpose == OtpPurpose.Register)
        {
            var user = await _uow.Users.GetByEmailAsync(request.Email);
            if (user != null)
            {
                user.IsEmailVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                _uow.Users.Update(user);
            }
        }

        await _uow.SaveChangesAsync();
        return (true, purpose == OtpPurpose.Register ? "Account verified successfully. You can now log in." : "OTP verified successfully.");
    }

    public async Task<(bool Success, string? Message)> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _uow.Users.GetByEmailAsync(request.Email);
        if (existingUser != null)
            return (false, "Email already registered");

        var user = new User
        {
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            IsEmailVerified = false
        };
        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        var otpCode = GenerateOtpCode();
        var otp = new OtpVerification
        {
            Email = request.Email,
            OtpCode = otpCode,
            Purpose = OtpPurpose.Register,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes)
        };
        await _uow.OtpVerifications.AddAsync(otp);
        await _uow.SaveChangesAsync();

        var subject = "Verify your email - Bonddy Platform";
        var body = $"<p>Welcome, {request.FullName}!</p><p>Your verification code is: <strong>{otpCode}</strong></p><p>Valid for {OtpExpiryMinutes} minutes. Use the Verify OTP API to verify your account.</p>";
        await _emailSender.SendEmailAsync(request.Email, subject, body);

        return (true, "Registration successful. Check your email for the verification OTP, then call Verify OTP to verify your account.");
    }

    public async Task<(bool Success, string? Message)> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email);
        if (user == null)
            return (true, "If the email exists, an OTP has been sent."); // Don't reveal existence

        // Client should call RequestOtp with Purpose=ForgotPassword, then call ResetPassword with OTP
        return (true, "If the email exists, request OTP with purpose ForgotPassword then reset password.");
    }

    public async Task<(bool Success, string? Message)> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var otp = await _uow.OtpVerifications.GetLatestValidAsync(request.Email, OtpPurpose.ForgotPassword);
        if (otp == null)
            return (false, "Invalid or expired OTP. Please request a new OTP.");
        if (otp.OtpCode != request.OtpCode.Trim())
            return (false, "Invalid OTP code");

        var user = await _uow.Users.GetByEmailAsync(request.Email);
        if (user == null)
            return (false, "User not found");

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        otp.IsUsed = true;
        _uow.OtpVerifications.Update(otp);
        await _uow.SaveChangesAsync();
        return (true, "Password reset successfully");
    }

    public async Task<(bool Success, string? Message)> ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
            return (false, "User not found");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            return (false, "Current password is incorrect");

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
        return (true, "Password changed successfully");
    }

    public async Task<(bool Success, string? Message, LoginResponseDto? Data)> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return (false, "Refresh token is required", null);

        var stored = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken.Trim());
        if (stored == null || !stored.IsActive)
            return (false, "Invalid or expired refresh token", null);

        stored.RevokedAt = DateTime.UtcNow;
        _uow.RefreshTokens.Update(stored);

        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshTokenValue();
        var newRefreshToken = new RefreshToken
        {
            UserId = stored.UserId,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays)
        };
        await _uow.RefreshTokens.AddAsync(newRefreshToken);
        await _uow.SaveChangesAsync();

        var user = stored.User;
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var response = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresInSeconds = _jwtTokenGenerator.AccessTokenExpirySeconds,
            TokenType = "Bearer"
        };
        return (true, null, response);
    }

    private static OtpPurpose? ParseOtpPurpose(string purpose)
    {
        if (string.Equals(purpose, "Register", StringComparison.OrdinalIgnoreCase))
            return OtpPurpose.Register;
        if (string.Equals(purpose, "ForgotPassword", StringComparison.OrdinalIgnoreCase))
            return OtpPurpose.ForgotPassword;
        return null;
    }

    private static string GenerateOtpCode()
    {
        var rnd = new Random();
        return rnd.Next(100000, 999999).ToString();
    }
}
