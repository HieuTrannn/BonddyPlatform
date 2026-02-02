using System.Linq;
using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IUserRepository
{
    IQueryable<User> GetQueryable();
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task AddAsync(User user);
    void Update(User user);
    void Remove(User user);
}
