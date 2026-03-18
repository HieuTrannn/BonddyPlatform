using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.ReviewDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IReviewService
{
    /// <summary>Traveler tạo review cho booking đã Completed</summary>
    Task<ReviewResponseDto> CreateAsync(Guid reviewerUserId, ReviewCreateRequestDto dto);

    /// <summary>Lấy review theo bookingId</summary>
    Task<ReviewResponseDto?> GetByBookingIdAsync(Guid bookingId);

    /// <summary>Public reviews for buddy profile</summary>
    Task<PagedResultDto<ReviewResponseDto>> GetPublicByBuddyIdAsync(Guid buddyId, int page, int pageSize);
}
