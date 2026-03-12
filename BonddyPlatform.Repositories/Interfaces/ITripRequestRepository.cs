using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface ITripRequestRepository : IGenericRepository<TripRequest>
{
    /// <summary>All requests for a trip, including Buddy + User info.</summary>
    Task<IEnumerable<TripRequest>> GetByTripIdAsync(Guid tripId);

    /// <summary>All requests sent by a buddy, including Trip info.</summary>
    Task<IEnumerable<TripRequest>> GetByBuddyIdAsync(Guid buddyId);

    /// <summary>Returns the single request a buddy made for a specific trip (if any).</summary>
    Task<TripRequest?> GetByTripAndBuddyAsync(Guid tripId, Guid buddyId);

    /// <summary>Returns a request with Trip + Buddy + User navigation loaded.</summary>
    Task<TripRequest?> GetWithDetailsAsync(Guid id);
}
