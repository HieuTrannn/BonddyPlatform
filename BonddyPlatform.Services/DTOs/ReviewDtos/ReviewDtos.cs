namespace BonddyPlatform.Services.DTOs.ReviewDtos;

public class ReviewCreateRequestDto
{
    public Guid BookingId { get; set; }
    /// <summary>1–5</summary>
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class ReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
}
