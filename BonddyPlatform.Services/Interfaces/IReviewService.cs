using BonddyPlatform.Services.DTOs.ReviewDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IReviewService
{
    /// <summary>Traveler tạo review cho booking đã Completed</summary>
    Task<ReviewResponseDto> CreateAsync(Guid reviewerUserId, ReviewCreateRequestDto dto);

    /// <summary>Lấy review theo bookingId</summary>
    Task<ReviewResponseDto?> GetByBookingIdAsync(Guid bookingId);
}
