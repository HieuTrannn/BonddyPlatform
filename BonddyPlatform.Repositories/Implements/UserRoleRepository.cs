using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly BonddyDbContext _context;

    public UserRoleRepository(BonddyDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId)
        => await _context.UserRoles.Where(r => r.UserId == userId).ToListAsync();

    public async Task<bool> HasRoleAsync(Guid userId, Role role)
        => await _context.UserRoles.AnyAsync(r => r.UserId == userId && r.Role == role);

    public async Task AddAsync(UserRole userRole)
        => await _context.UserRoles.AddAsync(userRole);

    public void Remove(UserRole userRole)
        => _context.UserRoles.Remove(userRole);

    public async Task<UserRole?> GetAsync(Guid userId, Role role)
        => await _context.UserRoles.FirstOrDefaultAsync(r => r.UserId == userId && r.Role == role);
}
