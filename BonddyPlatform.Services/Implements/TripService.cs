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
    private readonly IChatService _chatService;

    public TripService(IUnitOfWork uow, IChatService chatService)
    {
        _uow = uow;
        _chatService = chatService;
    }

    public async Task<TripResponseDto> CreateAsync(Guid travelerUserId, TripCreateRequestDto dto)
    {
        var entity = new Trip
        {
            City = dto.City.Trim(),
            StartTime = dto.StartTime,
            DurationMinutes = dto.DurationMinutes,
            GroupSize = dto.GroupSize,
            PreferredLanguage = dto.PreferredLanguage?.Trim(),
            BudgetMin = dto.BudgetMin,
            BudgetMax = dto.BudgetMax,
            MeetingPoint = dto.MeetingPoint?.Trim(),
            Notes = dto.Notes?.Trim(),
            Status = TripStatus.Submitted,
            TravelerUserId = travelerUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Trips.AddAsync(entity);
        await _uow.SaveChangesAsync();

        // Auto create chat room for this trip
        await _chatService.GetOrCreateTripRoomAsync(entity.Id, travelerUserId);

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<TripResponseDto>> GetMyTripsAsync(Guid travelerUserId, TripSearchRequestDto request)
    {
        var query = _uow.Trips.GetQueryable()
            .Where(t => t.TravelerUserId == travelerUserId);

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

    public async Task<TripResponseDto?> GetByIdAsync(Guid id)
    {
        var entity = await _uow.Trips.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    private static IQueryable<Trip> ApplyFilters(IQueryable<Trip> query, TripSearchRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(t => t.City == request.City);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.StartFrom.HasValue)
        {
            query = query.Where(t => t.StartTime >= request.StartFrom.Value);
        }

        if (request.StartTo.HasValue)
        {
            query = query.Where(t => t.StartTime <= request.StartTo.Value);
        }

        return query;
    }

    private static TripResponseDto MapToDto(Trip t) => new()
    {
        Id = t.Id,
        City = t.City,
        StartTime = t.StartTime,
        DurationMinutes = t.DurationMinutes,
        GroupSize = t.GroupSize,
        PreferredLanguage = t.PreferredLanguage,
        BudgetMin = t.BudgetMin,
        BudgetMax = t.BudgetMax,
        MeetingPoint = t.MeetingPoint,
        Notes = t.Notes,
        Status = t.Status,
        TravelerUserId = t.TravelerUserId,
        CreatedAt = t.CreatedAt
    };
}

