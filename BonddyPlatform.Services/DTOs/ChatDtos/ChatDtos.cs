using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.DTOs.ChatDtos;

public class ChatRoomDto
{
    public Guid Id { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public Guid? TripId { get; set; }
    public Guid? TripRequestId { get; set; }
    public Guid? BuddyUserId { get; set; }
    /// <summary>UserId of the other participant (the person you are chatting with).</summary>
    public Guid? OtherUserId { get; set; }
    /// <summary>Full name of the other participant.</summary>
    public string? OtherUserName { get; set; }
    /// <summary>Profile picture URL of the other participant.</summary>
    public string? OtherUserProfilePicture { get; set; }
    /// <summary>Preview text of the most recent message in this room.</summary>
    public string? LastMessage { get; set; }
    /// <summary>Timestamp of the most recent message in this room.</summary>
    public DateTime? LastMessageAt { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid SenderUserId { get; set; }
    /// <summary>Full name of the sender.</summary>
    public string? SenderFullName { get; set; }
    /// <summary>Profile picture URL of the sender.</summary>
    public string? SenderProfilePicture { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ContentText { get; set; }
    /// <summary>Non-null when ContentType = BookingCard.</summary>
    public Guid? BookingId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response for GetMessages: includes other participant info and paginated messages.
/// </summary>
public class ChatRoomMessagesDto
{
    /// <summary>UserId of the other participant in this room.</summary>
    public Guid? OtherUserId { get; set; }
    /// <summary>Full name of the other participant.</summary>
    public string? OtherUserName { get; set; }
    /// <summary>Profile picture URL of the other participant.</summary>
    public string? OtherUserProfilePicture { get; set; }
    /// <summary>Whether the other participant is currently online.</summary>
    public bool IsOnline { get; set; }
    public IEnumerable<MessageDto> Items { get; set; } = Enumerable.Empty<MessageDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class SendMessageRequestDto
{
    public string ContentText { get; set; } = string.Empty;
}

