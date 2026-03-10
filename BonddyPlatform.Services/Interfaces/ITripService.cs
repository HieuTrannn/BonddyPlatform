using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.TripDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface ITripService
{
    Task<TripResponseDto> CreateAsync(Guid travelerUserId, TripCreateRequestDto dto);
    Task<PagedResultDto<TripResponseDto>> GetMyTripsAsync(Guid travelerUserId, TripSearchRequestDto request);
    Task<PagedResultDto<TripResponseDto>> GetTripsForOpsAsync(TripSearchRequestDto request);
    Task<TripResponseDto?> GetByIdAsync(Guid id);
}
