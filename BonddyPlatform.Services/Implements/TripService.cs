using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.TripDtos;
using BonddyPlatform.Services.Helpers;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Services.Implements;

public class TripService : ITripService
{
    private readonly IUnitOfWork _uow;

    public TripService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<TripResponseDto> CreateAsync(Guid travelerUserId, TripCreateRequestDto dto)
    {
        var entity = new Trip
        {
            City = dto.City.Trim(),
            StartDate = dto.StartDate,
            StartTime = dto.StartTime,
            DurationHours = dto.DurationHours,
            Adults = dto.Adults,
            Children = dto.Children,
            PreferredLanguages = dto.PreferredLanguages,
            Notes = dto.Notes?.Trim(),
            Status = TripStatus.Active,
            TravelerUserId = travelerUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Trips.AddAsync(entity);
        await _uow.SaveChangesAsync();

        // Reload with TravelerUser
        var created = await _uow.Trips.GetByIdAsync(entity.Id);
        return MapToDto(created ?? entity);
    }

    public async Task<TripResponseDto> UpdateAsync(Guid tripId, Guid requesterId, TripUpdateRequestDto dto)
    {
        var entity = await _uow.Trips.GetByIdAsync(tripId);
        if (entity == null) throw new KeyNotFoundException("Trip not found.");
        if (entity.TravelerUserId != requesterId)
            throw new UnauthorizedAccessException("You are not allowed to update this trip.");

        if (dto.StartDate.HasValue) entity.StartDate = dto.StartDate.Value;
        if (dto.StartTime.HasValue) entity.StartTime = dto.StartTime.Value;
        if (dto.DurationHours.HasValue) entity.DurationHours = dto.DurationHours.Value;
        if (dto.Adults.HasValue) entity.Adults = dto.Adults.Value;
        if (dto.Children.HasValue) entity.Children = dto.Children.Value;
        if (dto.PreferredLanguages != null) entity.PreferredLanguages = dto.PreferredLanguages;
        if (dto.Notes != null) entity.Notes = dto.Notes.Trim();

        entity.UpdatedAt = DateTime.UtcNow;
        _uow.Trips.Update(entity);
        await _uow.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<TripResponseDto>> GetMyTripsAsync(Guid travelerUserId, TripSearchRequestDto request)
    {
        var query = _uow.Trips.GetQueryable()
            .Where(t => t.TravelerUserId == travelerUserId && t.Status != TripStatus.Deleted);

        query = ApplyFilters(query, request);

        var totalCount = await query.CountAsync();
        query = query.ApplySorting(request.SortBy ?? nameof(Trip.CreatedAt), request.SortOrder);
        var items = await query.ApplyPaging(request.Page, request.PageSize).ToListAsync();

        return new PagedResultDto<TripResponseDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<PagedResultDto<TripResponseDto>> GetTripsForOpsAsync(TripSearchRequestDto request)
    {
        var query = _uow.Trips.GetQueryable();
        query = ApplyFilters(query, request);

        var totalCount = await query.CountAsync();
        query = query.ApplySorting(request.SortBy ?? nameof(Trip.CreatedAt), request.SortOrder);
        var items = await query.ApplyPaging(request.Page, request.PageSize).ToListAsync();

        return new PagedResultDto<TripResponseDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>Open trips that buddies can see and send requests to.</summary>
    public async Task<PagedResultDto<TripResponseDto>> GetOpenTripsForBuddyAsync(TripSearchRequestDto request)
    {
        var query = _uow.Trips.GetQueryable()
            .Where(t => t.Status == TripStatus.Active);  // Deleted trips never appear here

        query = ApplyFilters(query, request);

        var totalCount = await query.CountAsync();
        query = query.ApplySorting(request.SortBy ?? nameof(Trip.CreatedAt), request.SortOrder);
        var items = await query.ApplyPaging(request.Page, request.PageSize).ToListAsync();

        return new PagedResultDto<TripResponseDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<TripResponseDto?> GetByIdAsync(Guid id)
    {
        var entity = await _uow.Trips.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task DeleteAsync(Guid tripId, Guid requesterId)
    {
        var entity = await _uow.Trips.GetByIdAsync(tripId);
        if (entity == null) throw new KeyNotFoundException("Trip not found.");
        if (entity.TravelerUserId != requesterId)
            throw new UnauthorizedAccessException("You are not allowed to delete this trip.");
        if (entity.Status == TripStatus.Deleted)
            throw new InvalidOperationException("Trip is already deleted.");

        // Không cho xóa nếu đang có booking active
        var activeBookings = await _uow.Bookings.GetByTripIdAsync(tripId);
        var hasActive = activeBookings.Any(b =>
            b.Status is BookingStatus.PendingCustomerConfirm
                     or BookingStatus.PendingPayment
                     or BookingStatus.Confirmed
                     or BookingStatus.InProgress);
        if (hasActive)
            throw new InvalidOperationException("Cannot delete trip with active bookings.");

        entity.Status    = TripStatus.Deleted;
        entity.UpdatedAt = DateTime.UtcNow;
        _uow.Trips.Update(entity);
        await _uow.SaveChangesAsync();
    }

    private static IQueryable<Trip> ApplyFilters(IQueryable<Trip> query, TripSearchRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(t => t.City == request.City);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.StartFrom.HasValue)
            query = query.Where(t => t.StartDate >= request.StartFrom.Value);

        if (request.StartTo.HasValue)
            query = query.Where(t => t.StartDate <= request.StartTo.Value);

        return query;
    }

    private static TripResponseDto MapToDto(Trip t) => new()
    {
        Id = t.Id,
        City = t.City,
        StartDate = t.StartDate,
        StartTime = t.StartTime,
        DurationHours = t.DurationHours,
        Adults = t.Adults,
        Children = t.Children,
        PreferredLanguages = t.PreferredLanguages,
        Notes = t.Notes,
        Status = t.Status,
        TravelerUserId = t.TravelerUserId,
        TravelerName = t.TravelerUser?.FullName ?? string.Empty,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
