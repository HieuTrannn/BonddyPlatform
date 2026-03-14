using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IUserRoleRepository
{
    Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId);
    Task<bool> HasRoleAsync(Guid userId, Role role);
    Task AddAsync(UserRole userRole);
    void Remove(UserRole userRole);
    Task<UserRole?> GetAsync(Guid userId, Role role);
}
