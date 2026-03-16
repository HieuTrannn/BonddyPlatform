namespace BonddyPlatform.Services.DTOs.ChatDtos;

public class ChatRoomDto
{
    public Guid Id { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public Guid? TripId { get; set; }
    public Guid? TripRequestId { get; set; }
    public Guid? BuddyUserId { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid SenderUserId { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ContentText { get; set; }
    /// <summary>Non-null when ContentType = BookingCard.</summary>
    public Guid? BookingId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendMessageRequestDto
{
    public string ContentText { get; set; } = string.Empty;
}
