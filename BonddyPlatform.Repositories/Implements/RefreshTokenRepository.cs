using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly BonddyDbContext _context;

    public RefreshTokenRepository(BonddyDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
        => await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token);

    public async Task AddAsync(RefreshToken refreshToken)
        => await _context.RefreshTokens.AddAsync(refreshToken);

    public void Update(RefreshToken refreshToken)
        => _context.RefreshTokens.Update(refreshToken);
}
