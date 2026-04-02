using BonddyPlatform.Services.DTOs.ServicePackageDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IServicePackageService
{
    // ─── Public ──────────────────────────────────────────────────────────────
    Task<IEnumerable<ServicePackageResponseDto>> GetAllActiveAsync();
    Task<ServicePackageResponseDto?> GetByIdAsync(Guid id);

    // ─── Admin CRUD ──────────────────────────────────────────────────────────
    Task<IEnumerable<ServicePackageResponseDto>> GetAllAsync();
    Task<ServicePackageResponseDto> CreateAsync(ServicePackageCreateDto dto);
    Task<ServicePackageResponseDto?> UpdateAsync(Guid id, ServicePackageUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);

    // ─── Buddy Subscription + Payment ───────────────────────────────────────

    /// <summary>
    /// Buddy đăng ký/nâng cấp gói. Tự động xử lý:
    /// - Chưa có gói → tạo mới, PayPal full price
    /// - Upgrade (gói cao hơn) → proration, charge chênh lệch, kích hoạt ngay
    /// - Downgrade (gói thấp hơn) → schedule đổi gói khi hết kỳ hiện tại
    /// </summary>
    Task<object> SubscribeAsync(Guid userId, SubscribeRequestDto dto);

    /// <summary>Capture PayPal order → kích hoạt subscription.</summary>
    Task<BuddySubscriptionResponseDto> CaptureSubscriptionPaymentAsync(Guid userId, Guid subscriptionId, SubscriptionCaptureRequestDto dto);

    /// <summary>Xem gói đang active.</summary>
    Task<BuddySubscriptionResponseDto?> GetMySubscriptionAsync(Guid userId);

    /// <summary>
    /// Hủy gói — giữ active đến hết kỳ thanh toán, sau đó không gia hạn.
    /// </summary>
    Task<BuddySubscriptionResponseDto?> CancelSubscriptionAsync(Guid userId);
}
