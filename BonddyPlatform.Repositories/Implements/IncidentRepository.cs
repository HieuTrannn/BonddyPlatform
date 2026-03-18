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

    public async Task<(IEnumerable<Incident> Items, int TotalCount)> SearchAsync(
        IncidentStatus? status,
        IncidentType? type,
        Guid? bookingId,
        int page,
        int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Incidents
            .AsNoTracking()
            .Include(i => i.ReportedByUser)
            .AsQueryable();

        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (type.HasValue) query = query.Where(i => i.Type == type.Value);
        if (bookingId.HasValue) query = query.Where(i => i.BookingId == bookingId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
