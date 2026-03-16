namespace BonddyPlatform.Services.DTOs.NotificationDtos;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class NotificationListResponseDto
{
    public IEnumerable<NotificationDto> Items { get; set; } = Array.Empty<NotificationDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class UnreadCountDto
{
    public int UnreadCount { get; set; }
}

public class ChatUnreadRoomDto
{
    public Guid RoomId { get; set; }
    public int UnreadCount { get; set; }
    public DateTime LastReadAt { get; set; }
}

public class ChatUnreadSummaryDto
{
    public int TotalUnread { get; set; }
    public IEnumerable<ChatUnreadRoomDto> Rooms { get; set; } = Array.Empty<ChatUnreadRoomDto>();
}

