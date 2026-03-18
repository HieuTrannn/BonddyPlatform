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

    public async Task<(IEnumerable<Review> Items, int TotalCount)> GetPublicByBuddyIdAsync(Guid buddyId, int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 50) pageSize = 50;

        var query = _context.Reviews
            .AsNoTracking()
            .Include(r => r.ReviewerUser)
            .Join(
                _context.Bookings.AsNoTracking(),
                r => r.BookingId,
                b => b.Id,
                (r, b) => new { Review = r, Booking = b })
            .Where(x => x.Booking.BuddyId == buddyId && x.Review.IsPublic);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Review.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Review)
            .ToListAsync();

        return (items, total);
    }
}
