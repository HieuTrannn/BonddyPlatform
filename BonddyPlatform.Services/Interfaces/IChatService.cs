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

    Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId);

    Task<PagedResultDto<MessageDto>> GetMessagesAsync(Guid roomId, int page, int pageSize);

    Task<MessageDto> SendMessageAsync(Guid roomId, Guid senderUserId, SendMessageRequestDto request);

    /// <summary>
    /// Sends a BookingCard message into the existing chat room.
    /// senderUserId must already be a participant.
    /// </summary>
    Task<MessageDto> SendBookingCardAsync(Guid roomId, Guid senderUserId, Guid bookingId);
}
