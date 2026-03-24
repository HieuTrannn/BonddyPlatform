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

    /// <summary>Create PayPal order for a booking payment.</summary>
    Task<PayPalCreateOrderResponseDto> CreatePayPalOrderAsync(Guid bookingId, Guid requesterUserId);

    /// <summary>Capture a PayPal order and mark payment as held.</summary>
    Task<PaymentResponseDto> CapturePayPalOrderAsync(Guid paymentId, string orderId, Guid requesterUserId);

    /// <summary>Handle PayPal webhook (signature verification + state updates).</summary>
    Task<PayPalWebhookResultDto> HandlePayPalWebhookAsync(string rawBody, IDictionary<string, string> headers);
}
