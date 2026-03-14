using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.BookingDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IBookingService
{
    /// <summary>CS/Admin tạo booking cho một trip</summary>
    Task<BookingResponseDto> CreateForTripAsync(Guid createdByUserId, BookingCreateRequestDto dto);

    /// <summary>Lấy toàn bộ bookings của một trip</summary>
    Task<IEnumerable<BookingResponseDto>> GetByTripIdAsync(Guid tripId);

    /// <summary>Lấy chi tiết một booking</summary>
    Task<BookingResponseDto?> GetByIdAsync(Guid id);

    /// <summary>CS/Admin cập nhật trạng thái booking (state machine)</summary>
    Task<BookingResponseDto?> UpdateStatusAsync(Guid id, BookingStatus newStatus);
}
