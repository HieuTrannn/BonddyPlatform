using BonddyPlatform.Services.DTOs.PaymentDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IPaymentService
{
    /// <summary>CS/Admin tạo payment record cho một booking (khởi tạo ở trạng thái Pending)</summary>
    Task<PaymentResponseDto> CreateAsync(PaymentCreateRequestDto dto);

    /// <summary>Lấy payment theo bookingId</summary>
    Task<PaymentResponseDto?> GetByBookingIdAsync(Guid bookingId);

    /// <summary>Lấy payment theo id</summary>
    Task<PaymentResponseDto?> GetByIdAsync(Guid id);

    /// <summary>CS/Admin chuyển trạng thái: Pending → Held → Released / Refunded</summary>
    Task<PaymentResponseDto?> TransitionAsync(Guid id, PaymentTransitionRequestDto dto);
}
