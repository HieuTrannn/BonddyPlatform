using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class BookingRepository : GenericRepository<Booking>, IBookingRepository
{
    public BookingRepository(BonddyDbContext context) : base(context) { }

    public async Task<Booking?> GetByIdWithDetailsAsync(Guid id)
        => await _context.Bookings
            .Include(b => b.Trip)
            .Include(b => b.TripRequest)
            .Include(b => b.Buddy).ThenInclude(bp => bp.User)
            .Include(b => b.TravelerUser)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Booking?> GetByTripRequestIdAsync(Guid tripRequestId)
        => await _context.Bookings
            .Include(b => b.Trip)
            .Include(b => b.Buddy).ThenInclude(bp => bp.User)
            .Include(b => b.TravelerUser)
            .FirstOrDefaultAsync(b => b.TripRequestId == tripRequestId);

    public async Task<IEnumerable<Booking>> GetByTripIdAsync(Guid tripId)
        => await _context.Bookings
            .Include(b => b.Buddy).ThenInclude(bp => bp.User)
            .Include(b => b.TravelerUser)
            .Where(b => b.TripId == tripId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetByTravelerUserIdAsync(Guid travelerUserId)
        => await _context.Bookings
            .Include(b => b.Trip)
            .Include(b => b.Buddy).ThenInclude(bp => bp.User)
            .Where(b => b.TravelerUserId == travelerUserId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetByBuddyIdAsync(Guid buddyId)
        => await _context.Bookings
            .Include(b => b.Trip)
            .Include(b => b.TravelerUser)
            .Where(b => b.BuddyId == buddyId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
}
