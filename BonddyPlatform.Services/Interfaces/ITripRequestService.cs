using BonddyPlatform.Services.DTOs.TripDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface ITripRequestService
{
    /// <summary>Buddy sends an offer to an open Trip.</summary>
    Task<TripRequestResponseDto> CreateAsync(Guid buddyUserId, TripRequestCreateDto dto);

    /// <summary>Returns all requests for a trip (traveler sees buddy offers).</summary>
    Task<IEnumerable<TripRequestResponseDto>> GetByTripIdAsync(Guid tripId, Guid requesterId);

    /// <summary>Returns all requests sent by the current buddy.</summary>
    Task<IEnumerable<TripRequestResponseDto>> GetMyRequestsAsync(Guid buddyUserId);

    /// <summary>Buddy withdraws their own offer.</summary>
    Task DeleteAsync(Guid requestId, Guid buddyUserId);

    /// <summary>
    /// Traveler clicks "Message" on a buddy's offer.
    /// Creates (or returns existing) 1:1 DirectBuddy chat room.
    /// </summary>
    Task<StartChatResponseDto> StartChatAsync(Guid requestId, Guid travelerUserId);
}
