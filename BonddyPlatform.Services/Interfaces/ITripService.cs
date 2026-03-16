using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.TripDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface ITripService
{
    Task<TripResponseDto> CreateAsync(Guid travelerUserId, TripCreateRequestDto dto);
    Task<TripResponseDto> UpdateAsync(Guid tripId, Guid requesterId, TripUpdateRequestDto dto);
    Task<PagedResultDto<TripResponseDto>> GetMyTripsAsync(Guid travelerUserId, TripSearchRequestDto request);
    Task<PagedResultDto<TripResponseDto>> GetTripsForOpsAsync(TripSearchRequestDto request);
    Task<PagedResultDto<TripResponseDto>> GetOpenTripsForBuddyAsync(TripSearchRequestDto request);
    Task<TripResponseDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Traveler xóa trip — soft delete, chỉ cập nhật status thành Deleted.
    /// Không cho xóa nếu trip đang có booking active.
    /// </summary>
    Task DeleteAsync(Guid tripId, Guid requesterId);
}
