using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Services.DTOs.IncidentDtos;

public class IncidentSearchRequestDto
{
    public IncidentStatus? Status { get; set; }
    public IncidentType? Type { get; set; }
    public Guid? BookingId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

