using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IBookingRepository : IGenericRepository<Booking>
{
    Task<Booking?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<Booking>> GetByTripIdAsync(Guid tripId);
}
