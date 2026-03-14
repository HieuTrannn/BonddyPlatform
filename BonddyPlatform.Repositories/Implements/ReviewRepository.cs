using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<Review?> GetByBookingIdAsync(Guid bookingId)
        => await _context.Reviews
            .Include(r => r.ReviewerUser)
            .FirstOrDefaultAsync(r => r.BookingId == bookingId);
}
