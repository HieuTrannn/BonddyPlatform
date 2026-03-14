using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IReviewRepository : IGenericRepository<Review>
{
    Task<Review?> GetByBookingIdAsync(Guid bookingId);
}
