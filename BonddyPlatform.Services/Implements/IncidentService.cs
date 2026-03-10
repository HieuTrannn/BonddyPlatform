using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.IncidentDtos;
using BonddyPlatform.Services.Interfaces;

namespace BonddyPlatform.Services.Implements;

public class IncidentService : IIncidentService
{
    private readonly IUnitOfWork _uow;

    private static readonly HashSet<IncidentStatus> _resolvedStatuses =
        new() { IncidentStatus.Resolved, IncidentStatus.Closed };

    public IncidentService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IncidentResponseDto> CreateAsync(Guid reportedByUserId, IncidentCreateRequestDto dto)
    {
        var booking = await _uow.Bookings.GetByIdAsync(dto.BookingId)
            ?? throw new KeyNotFoundException($"Booking {dto.BookingId} not found.");

        var incident = new Incident
        {
            BookingId = dto.BookingId,
            ReportedByUserId = reportedByUserId,
            Type = dto.Type,
            Description = dto.Description,
            Status = IncidentStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Incidents.AddAsync(incident);
        await _uow.SaveChangesAsync();

        var incidents = await _uow.Incidents.GetByBookingIdAsync(dto.BookingId);
        var full = incidents.First(i => i.Id == incident.Id);
        return ToDto(full);
    }

    public async Task<IEnumerable<IncidentResponseDto>> GetByBookingIdAsync(Guid bookingId)
    {
        var incidents = await _uow.Incidents.GetByBookingIdAsync(bookingId);
        return incidents.Select(ToDto);
    }

    public async Task<IncidentResponseDto?> ResolveAsync(Guid id, IncidentResolveRequestDto dto)
    {
        var incident = await _uow.Incidents.GetByIdAsync(id);
        if (incident is null) return null;

        incident.Status = dto.Status;
        if (dto.Resolution is not null) incident.Resolution = dto.Resolution;

        if (_resolvedStatuses.Contains(dto.Status))
            incident.ResolvedAt = DateTime.UtcNow;

        _uow.Incidents.Update(incident);
        await _uow.SaveChangesAsync();

        var incidents = await _uow.Incidents.GetByBookingIdAsync(incident.BookingId);
        var full = incidents.First(i => i.Id == id);
        return ToDto(full);
    }

    private static IncidentResponseDto ToDto(Incident i) => new()
    {
        Id = i.Id,
        BookingId = i.BookingId,
        ReportedByUserId = i.ReportedByUserId,
        ReportedByName = i.ReportedByUser?.FullName ?? string.Empty,
        Type = i.Type,
        Description = i.Description,
        Status = i.Status,
        Resolution = i.Resolution,
        CreatedAt = i.CreatedAt,
        ResolvedAt = i.ResolvedAt
    };
}
