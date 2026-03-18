using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IIncidentRepository : IGenericRepository<Incident>
{
    Task<IEnumerable<Incident>> GetByBookingIdAsync(Guid bookingId);

    Task<(IEnumerable<Incident> Items, int TotalCount)> SearchAsync(
        IncidentStatus? status,
        IncidentType? type,
        Guid? bookingId,
        int page,
        int pageSize);
}
