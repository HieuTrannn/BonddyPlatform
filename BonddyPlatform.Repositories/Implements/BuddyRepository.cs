using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class BuddyRepository : GenericRepository<Buddy>, IBuddyRepository
{
    public BuddyRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<Buddy?> GetByUserIdAsync(Guid userId)
        => await _context.Buddies
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.UserId == userId);

    public async Task<IEnumerable<Buddy>> GetAllActiveAsync()
        => await _context.Buddies
            .Include(b => b.User)
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.Rate)
            .ToListAsync();

    public async Task<Buddy?> GetByIdWithUserAsync(Guid id)
        => await _context.Buddies
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);
}
