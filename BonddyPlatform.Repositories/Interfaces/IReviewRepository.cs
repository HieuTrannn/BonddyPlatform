using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IReviewRepository : IGenericRepository<Review>
{
    Task<Review?> GetByBookingIdAsync(Guid bookingId);

    Task<(IEnumerable<Review> Items, int TotalCount)> GetPublicByBuddyIdAsync(Guid buddyId, int page, int pageSize);
}
