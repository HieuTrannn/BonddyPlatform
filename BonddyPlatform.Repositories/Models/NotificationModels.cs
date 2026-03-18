using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON payload for FE deep-linking (e.g. roomId, bookingId).
    /// </summary>
    public string? DataJson { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

public enum NotificationType
{
    ChatMessage = 0,
    BookingConfirmed = 1,
    PaymentSuccess = 2,
    TripCancelled = 3,
    TripRequestCreated = 4,
    IncidentUpdated = 5,
    System = 99
}

public class ChatRoomUserState
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }
    public ChatRoom Room { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Messages created at or before this timestamp are considered "read" for this user in this room.
    /// </summary>
    public DateTime LastReadAt { get; set; } = DateTime.UnixEpoch;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

