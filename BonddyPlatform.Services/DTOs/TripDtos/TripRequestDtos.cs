using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.TripDtos;

/// <summary>Buddy sends this to offer on a Trip.</summary>
public class TripRequestCreateDto
{
    [Required]
    public Guid TripId { get; set; }

    /// <summary>
    /// Total price buddy offers for the trip.
    /// Leave null and set IsInboxOnly = true to negotiate price in chat.
    /// </summary>
    public decimal? OfferedPrice { get; set; }

    /// <summary>True = buddy prefers to discuss price via chat ("Inbox").</summary>
    public bool IsInboxOnly { get; set; } = false;

    /// <summary>Intro message from buddy to the traveler.</summary>
    [MaxLength(500)]
    public string? Note { get; set; }
}

public class TripRequestResponseDto
{
    public Guid Id { get; set; }

    // Trip info
    public TripInfoResponseDto Trip { get; set; } = new();
    // Buddy info
    public BuddyInfoResponseDto Buddy { get; set; } = new();

    // Offer info
    public OfferInfoResponseDto Offer { get; set; } = new();

    /// <summary>True when the trip's start time has already passed.</summary>
    public bool IsExpired { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>If a chat room already exists for this request, its ID is returned here.</summary>
    public Guid? ExistingChatRoomId { get; set; }
}

public class TripInfoResponseDto
{
    public Guid Id { get; set; }
    public string City { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationHours { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
}

public class BuddyInfoResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

public class OfferInfoResponseDto
{
    public decimal Price { get; set; }
    public bool IsInboxOnly { get; set; }
    public string? Note { get; set; }
}


/// <summary>Response when traveler starts a chat from a TripRequest.</summary>
public class StartChatResponseDto
{
    public Guid ChatRoomId { get; set; }
    public Guid TripRequestId { get; set; }
    public Guid BuddyUserId { get; set; }
    public string BuddyName { get; set; } = string.Empty;
}
