using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Services.DTOs.IncidentDtos;

public class IncidentCreateRequestDto
{
    public Guid BookingId { get; set; }
    public IncidentType Type { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class IncidentResolveRequestDto
{
    public IncidentStatus Status { get; set; }
    public string? Resolution { get; set; }
}

public class IncidentResponseDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid ReportedByUserId { get; set; }
    public string ReportedByName { get; set; } = string.Empty;
    public IncidentType Type { get; set; }
    public string TypeName => Type.ToString();
    public string Description { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
