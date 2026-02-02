using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IOtpVerificationRepository
{
    Task<OtpVerification?> GetLatestValidAsync(string email, OtpPurpose purpose);
    Task AddAsync(OtpVerification otp);
    void Update(OtpVerification otp);
}
