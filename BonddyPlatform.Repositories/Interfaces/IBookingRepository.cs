using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IBookingRepository : IGenericRepository<Booking>
{
    Task<Booking?> GetByIdWithDetailsAsync(Guid id);
    Task<Booking?> GetByTripRequestIdAsync(Guid tripRequestId);
    Task<IEnumerable<Booking>> GetByTripIdAsync(Guid tripId);
    Task<IEnumerable<Booking>> GetByTravelerUserIdAsync(Guid travelerUserId);
    Task<IEnumerable<Booking>> GetByBuddyIdAsync(Guid buddyId);
}
