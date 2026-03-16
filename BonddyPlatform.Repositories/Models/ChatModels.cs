using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class ChatRoom
{
    public Guid Id { get; set; }

    public ChatRoomType RoomType { get; set; }

    /// <summary>
    /// Optional TripId to link this room with a Trip in future implementation.
    /// </summary>
    public Guid? TripId { get; set; }

    /// <summary>
    /// Optional buddy user id for direct buddy chat rooms.
    /// </summary>
    public Guid? BuddyUserId { get; set; }

    /// <summary>
    /// Links this DirectBuddy room to the TripRequest that triggered it.
    /// </summary>
    public Guid? TripRequestId { get; set; }
    public TripRequest? TripRequest { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ChatRoomParticipant> Participants { get; set; } = new List<ChatRoomParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class ChatRoomParticipant
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }
    public ChatRoom Room { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public RoomParticipantRole RoleInRoom { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class Message
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }
    public ChatRoom Room { get; set; } = null!;

    public Guid SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;

    public MessageContentType ContentType { get; set; } = MessageContentType.Text;

    [MaxLength(4000)]
    public string? ContentText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Guid? ReplyToMessageId { get; set; }
    public Message? ReplyToMessage { get; set; }

    /// <summary>Set khi ContentType = BookingCard để FE render booking card.</summary>
    public Guid? BookingId { get; set; }

    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
}

public class MessageAttachment
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }
    public Message Message { get; set; } = null!;

    [Required, MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ChatRoomType
{
    Trip = 0,
    DirectBuddy = 1
}

public enum RoomParticipantRole
{
    Traveler = 0,
    CS = 1,
    Buddy = 2
}

public enum MessageContentType
{
    Text = 0,
    Image = 1,
    File = 2,
    System = 3,
    /// <summary>Booking offer card sent by buddy; BookingId points to the Booking entity.</summary>
    BookingCard = 4
}
