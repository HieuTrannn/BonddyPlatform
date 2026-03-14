using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

/// <summary>
/// A Buddy's offer sent to an open Trip.
/// No accept/decline flow — traveler simply clicks "Message" to start chat.
/// Expires automatically when the Trip's start datetime passes.
/// </summary>
public class TripRequest
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public Guid BuddyId { get; set; }
    public Buddy Buddy { get; set; } = null!;

    /// <summary>
    /// Total price buddy offers for the trip.
    /// Null means buddy chose "Inbox" — will negotiate price in chat.
    /// </summary>
    public decimal? OfferedPrice { get; set; }

    /// <summary>True when buddy leaves price as "Inbox" (discuss in chat).</summary>
    public bool IsInboxOnly { get; set; } = false;

    /// <summary>Optional intro message from buddy to the traveler.</summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    /// <summary>
    /// Automatically set to the Trip's StartDate+StartTime (UTC).
    /// The request is considered expired once this passes.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
