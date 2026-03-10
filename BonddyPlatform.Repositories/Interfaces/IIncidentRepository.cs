using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IIncidentRepository : IGenericRepository<Incident>
{
    Task<IEnumerable<Incident>> GetByBookingIdAsync(Guid bookingId);
}
