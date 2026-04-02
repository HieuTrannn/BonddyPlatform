using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IBuddyRepository : IGenericRepository<Buddy>
{
    Task<Buddy?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Buddy>> GetAllActiveAsync();
    Task<Buddy?> GetByIdWithUserAsync(Guid id);

    /// <summary>Get all buddies with User included (for admin listing).</summary>
    Task<IEnumerable<Buddy>> GetAllWithUserAsync();
}
