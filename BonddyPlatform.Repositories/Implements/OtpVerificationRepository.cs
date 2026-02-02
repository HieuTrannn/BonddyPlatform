using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class OtpVerificationRepository : IOtpVerificationRepository
{
    private readonly BonddyDbContext _context;

    public OtpVerificationRepository(BonddyDbContext context)
    {
        _context = context;
    }

    public async Task<OtpVerification?> GetLatestValidAsync(string email, OtpPurpose purpose)
        => await _context.OtpVerifications
            .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task AddAsync(OtpVerification otp)
        => await _context.OtpVerifications.AddAsync(otp);

    public void Update(OtpVerification otp)
        => _context.OtpVerifications.Update(otp);
}
