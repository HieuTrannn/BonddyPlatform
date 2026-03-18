using BonddyPlatform.Services.DTOs.ChatDtos;
using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.Interfaces;

public interface IChatService
{
    Task<ChatRoomDto> GetOrCreateTripRoomAsync(Guid tripId, Guid travelerUserId);

    /// <summary>
    /// Traveler clicks "Message" on a TripRequest.
    /// Creates a DirectBuddy chat room between traveler and buddy, linked to that TripRequest.
    /// Idempotent — returns the existing room if already created.
    /// </summary>
    Task<ChatRoomDto> GetOrCreateDirectChatAsync(Guid tripRequestId, Guid travelerUserId, Guid buddyUserId);

    /// <summary>
    /// Traveler proactively starts a direct chat with a buddy, without any Trip.
    /// If a DirectBuddy room already exists between these two users, returns it.
    /// </summary>
    Task<ChatRoomDto> GetOrCreateDirectChatByBuddyUserIdAsync(Guid travelerUserId, Guid buddyUserId);

    Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId);

    Task<ChatRoomMessagesDto> GetMessagesAsync(Guid roomId, Guid currentUserId, int page, int pageSize);

    Task<MessageDto> SendMessageAsync(Guid roomId, Guid senderUserId, SendMessageRequestDto request);

    /// <summary>
    /// Sends a BookingCard message into the existing chat room.
    /// senderUserId must already be a participant.
    /// </summary>
    Task<MessageDto> SendBookingCardAsync(Guid roomId, Guid senderUserId, Guid bookingId);

    /// <summary>
    /// Returns all chat rooms that the given user participates in.
    /// </summary>
    Task<IEnumerable<ChatRoomDto>> GetRoomsForUserAsync(Guid userId);
}
