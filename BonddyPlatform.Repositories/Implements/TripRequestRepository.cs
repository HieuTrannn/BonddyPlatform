using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class TripRequestRepository : GenericRepository<TripRequest>, ITripRequestRepository
{
    public TripRequestRepository(BonddyDbContext context) : base(context) { }

    public async Task<IEnumerable<TripRequest>> GetByTripIdAsync(Guid tripId)
        => await _context.TripRequests
            .Include(r => r.Buddy).ThenInclude(b => b.User)
            .Where(r => r.TripId == tripId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<TripRequest>> GetByBuddyIdAsync(Guid buddyId)
        => await _context.TripRequests
            .Include(r => r.Trip).ThenInclude(t => t.TravelerUser)
            .Where(r => r.BuddyId == buddyId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<TripRequest?> GetByTripAndBuddyAsync(Guid tripId, Guid buddyId)
        => await _context.TripRequests
            .FirstOrDefaultAsync(r => r.TripId == tripId && r.BuddyId == buddyId);

    public async Task<TripRequest?> GetWithDetailsAsync(Guid id)
        => await _context.TripRequests
            .Include(r => r.Trip).ThenInclude(t => t.TravelerUser)
            .Include(r => r.Buddy).ThenInclude(b => b.User)
            .FirstOrDefaultAsync(r => r.Id == id);
}
