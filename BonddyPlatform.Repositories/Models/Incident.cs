using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class Incident
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    /// <summary>Người báo cáo sự cố (CS/Admin/Traveler)</summary>
    public Guid ReportedByUserId { get; set; }
    public User ReportedByUser { get; set; } = null!;

    public IncidentType Type { get; set; }

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    [MaxLength(2000)]
    public string? Resolution { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

public enum IncidentType
{
    NoShow = 0, // Không đến
    LateArrival = 1, // Đến muộn
    QualityIssue = 2, // Chất lượng dịch vụ
    SafetyConcern = 3, // An toàn
    PaymentDispute = 4, // Tranh chấp thanh toán
    Other = 5 // Khác
}

public enum IncidentStatus
{
    Open = 0,
    InReview = 1,
    Resolved = 2,
    Closed = 3
}
