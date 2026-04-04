using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.BookingDtos;
using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.Interfaces;

public interface IBookingService
{
    /// <summary>
    /// Buddy creates booking from chat. TripRequestId is optional.
    /// </summary>
    Task<BookingResponseDto> CreateAsync(Guid buddyUserId, BookingCreateRequestDto dto);

    /// <summary>
    /// Buddy tạo booking từ TripRequest (sau khi đã trao đổi trong chat).
    /// Tự động gửi booking card vào chat room giữa buddy và traveler.
    /// </summary>
    Task<BookingResponseDto> CreateFromTripRequestAsync(Guid buddyUserId, Guid tripRequestId, BookingCreateFromTripRequestDto dto);

    /// <summary>
    /// Traveler confirm booking → chuyển sang PendingPayment, set PaymentDeadline.
    /// </summary>
    Task<BookingResponseDto> ConfirmByTravelerAsync(Guid travelerUserId, Guid bookingId);

    /// <summary>
    /// Ensures booking is in PendingPayment for the traveler.
    /// If currently PendingCustomerConfirm, it will confirm.
    /// If already PendingPayment, returns current booking (idempotent).
    /// </summary>
    Task<BookingResponseDto> EnsurePendingPaymentAsync(Guid travelerUserId, Guid bookingId);

    /// <summary>Lấy chi tiết một booking.</summary>
    Task<BookingResponseDto?> GetByIdAsync(Guid id);

    /// <summary>Lấy toàn bộ bookings của một trip.</summary>
    Task<IEnumerable<BookingResponseDto>> GetByTripIdAsync(Guid tripId);

    /// <summary>Lấy tất cả bookings của traveler đang đăng nhập.</summary>
    Task<IEnumerable<BookingResponseDto>> GetMyBookingsAsTravelerAsync(Guid travelerUserId);

    /// <summary>Lấy tất cả bookings của buddy đang đăng nhập.</summary>
    Task<IEnumerable<BookingResponseDto>> GetMyBookingsAsBuddyAsync(Guid buddyUserId);

    /// <summary>CS/Admin cập nhật trạng thái booking (state machine).</summary>
    Task<BookingResponseDto?> UpdateStatusAsync(Guid id, BookingStatus newStatus);

    /// <summary>Admin lấy tất cả bookings với pagination và filter.</summary>
    Task<PagedResultDto<BookingResponseDto>> GetAllPagedAsync(BookingSearchRequestDto request);
}
