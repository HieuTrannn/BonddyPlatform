using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class BookingRepository : GenericRepository<Booking>, IBookingRepository
{
    public BookingRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<Booking?> GetByIdWithDetailsAsync(Guid id)
        => await _context.Bookings
            .Include(b => b.Trip)
            .Include(b => b.Guide)
            .Include(b => b.CreatedByUser)
            .Include(b => b.Surcharges)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IEnumerable<Booking>> GetByTripIdAsync(Guid tripId)
        => await _context.Bookings
            .Include(b => b.Guide)
            .Include(b => b.Surcharges)
            .Where(b => b.TripId == tripId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
}
