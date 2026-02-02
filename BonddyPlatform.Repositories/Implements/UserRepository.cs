using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class UserRepository : IUserRepository
{
    private readonly BonddyDbContext _context;

    public UserRepository(BonddyDbContext context)
    {
        _context = context;
    }

    public IQueryable<User> GetQueryable()
        => _context.Users.AsNoTracking();

    public async Task<IEnumerable<User>> GetAllAsync()
        => await _context.Users.AsNoTracking().ToListAsync();

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(int id)
        => await _context.Users.FindAsync(id);

    public async Task AddAsync(User user)
        => await _context.Users.AddAsync(user);

    public void Update(User user)
        => _context.Users.Update(user);

    public void Remove(User user)
        => _context.Users.Remove(user);
}
