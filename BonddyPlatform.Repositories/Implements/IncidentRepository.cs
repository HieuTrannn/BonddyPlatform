using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class IncidentRepository : GenericRepository<Incident>, IIncidentRepository
{
    public IncidentRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Incident>> GetByBookingIdAsync(Guid bookingId)
        => await _context.Incidents
            .Include(i => i.ReportedByUser)
            .Where(i => i.BookingId == bookingId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
}
