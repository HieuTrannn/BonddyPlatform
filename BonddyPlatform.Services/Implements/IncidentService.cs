using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.IncidentDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Services.Implements;

public class IncidentService : IIncidentService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    private static readonly HashSet<IncidentStatus> _resolvedStatuses =
        new() { IncidentStatus.Resolved, IncidentStatus.Closed };

    public IncidentService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

    public async Task<IncidentResponseDto> CreateAsync(Guid reportedByUserId, IncidentCreateRequestDto dto)
    {
        var booking = await _uow.Bookings.GetByIdAsync(dto.BookingId)
            ?? throw new KeyNotFoundException($"Booking {dto.BookingId} not found.");

        var isOps = await IsOpsAsync(reportedByUserId);
        var isTraveler = booking.TravelerUserId == reportedByUserId;
        var buddy = await _uow.Buddies.GetByIdAsync(booking.BuddyId);
        var isBuddy = buddy != null && buddy.UserId == reportedByUserId;

        if (!isOps && !isTraveler && !isBuddy)
            throw new UnauthorizedAccessException("You are not allowed to create an incident for this booking.");

        // Chỉ Admin/CS được report incident bất kỳ lúc nào.
        // Traveler/Buddy chỉ được report khi booking đã Completed.
        if (!isOps && booking.Status != BookingStatus.Completed)
            throw new InvalidOperationException(
                "Chỉ có thể báo cáo sự cố sau khi tour đã hoàn thành (Completed).");

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

        // Notify CS/Admin (ops) that a new incident was created
        await NotifyOpsAsync(
            title: "New incident reported",
            body: $"{dto.Type} incident reported for booking {booking.Id}.",
            data: new { incidentId = incident.Id, bookingId = booking.Id });

        var incidents = await _uow.Incidents.GetByBookingIdAsync(dto.BookingId);
        var full = incidents.First(i => i.Id == incident.Id);
        return ToDto(full);
    }

    public async Task<IEnumerable<IncidentResponseDto>> GetByBookingIdAsync(Guid requesterUserId, Guid bookingId)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        var isOps = await IsOpsAsync(requesterUserId);
        var isTraveler = booking.TravelerUserId == requesterUserId;
        var buddy = await _uow.Buddies.GetByIdAsync(booking.BuddyId);
        var isBuddy = buddy != null && buddy.UserId == requesterUserId;

        if (!isOps && !isTraveler && !isBuddy)
            throw new UnauthorizedAccessException("You are not allowed to view incidents for this booking.");

        var incidents = await _uow.Incidents.GetByBookingIdAsync(bookingId);
        return incidents.Select(ToDto);
    }

    public async Task<IncidentResponseDto?> ResolveAsync(Guid resolverUserId, Guid id, IncidentResolveRequestDto dto)
    {
        if (!await IsOpsAsync(resolverUserId))
            throw new UnauthorizedAccessException("Only CS/Admin can resolve incidents.");

        var incident = await _uow.Incidents.GetByIdAsync(id);
        if (incident is null) return null;

        incident.Status = dto.Status;
        if (dto.Resolution is not null) incident.Resolution = dto.Resolution;

        if (_resolvedStatuses.Contains(dto.Status))
            incident.ResolvedAt = DateTime.UtcNow;

        _uow.Incidents.Update(incident);
        await _uow.SaveChangesAsync();

        // Notify traveler and buddy about status update / resolution
        var booking = await _uow.Bookings.GetByIdAsync(incident.BookingId);
        if (booking != null)
        {
            var buddy = await _uow.Buddies.GetByIdAsync(booking.BuddyId);
            var dataJson = System.Text.Json.JsonSerializer.Serialize(new { incidentId = id, bookingId = booking.Id });
            var title = "Incident updated";
            var body = dto.Status is IncidentStatus.Resolved or IncidentStatus.Closed
                ? $"Your incident has been {dto.Status}. {dto.Resolution}".Trim()
                : $"Your incident status changed to {dto.Status}.";

            await _notificationService.CreateAsync(booking.TravelerUserId, NotificationType.IncidentUpdated, title, body, dataJson);
            if (buddy != null)
                await _notificationService.CreateAsync(buddy.UserId, NotificationType.IncidentUpdated, title, body, dataJson);
        }

        var incidents = await _uow.Incidents.GetByBookingIdAsync(incident.BookingId);
        var full = incidents.First(i => i.Id == id);
        return ToDto(full);
    }

    public async Task<PagedResultDto<IncidentResponseDto>> SearchForOpsAsync(Guid requesterUserId, IncidentSearchRequestDto request)
    {
        if (!await IsOpsAsync(requesterUserId))
            throw new UnauthorizedAccessException("Only CS/Admin can access incidents list.");

        var (items, total) = await _uow.Incidents.SearchAsync(
            status: request.Status,
            type: request.Type,
            bookingId: request.BookingId,
            page: request.Page,
            pageSize: request.PageSize);

        return new PagedResultDto<IncidentResponseDto>
        {
            Items = items.Select(ToDto),
            TotalCount = total,
            Page = request.Page <= 0 ? 1 : request.Page,
            PageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100)
        };
    }

    private async Task<bool> IsOpsAsync(Guid userId)
    {
        return await _uow.UserRoles.HasRoleAsync(userId, Role.Admin)
               || await _uow.UserRoles.HasRoleAsync(userId, Role.CS);
    }

    private async Task NotifyOpsAsync(string title, string body, object data)
    {
        var opsIds = await _uow.Users
            .GetQueryable()
            .Where(u => u.Roles.Any(r => r.Role == Role.Admin || r.Role == Role.CS))
            .Select(u => u.Id)
            .ToListAsync();

        var dataJson = System.Text.Json.JsonSerializer.Serialize(data);
        foreach (var id in opsIds)
        {
            await _notificationService.CreateAsync(id, NotificationType.System, title, body, dataJson);
        }
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
