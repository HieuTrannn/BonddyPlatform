using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.ServicePackageDtos;
using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;
using BonddyPlatform.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BonddyPlatform.Services.Implements;

public class ServicePackageService : IServicePackageService
{
    private readonly IUnitOfWork _uow;
    private readonly PayPalOptions _payPalOptions;
    private readonly ILogger<ServicePackageService> _logger;
    private readonly IPayOsService _payOsService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ServicePackageService(
        IUnitOfWork uow,
        IOptions<PayPalOptions> payPalOptions,
        ILogger<ServicePackageService> logger,
        IPayOsService payOsService,
        IHttpClientFactory httpClientFactory)
    {
        _uow = uow;
        _payPalOptions = payPalOptions.Value;
        _logger = logger;
        _payOsService = payOsService;
        _httpClientFactory = httpClientFactory;
    }

    // ─── Public ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ServicePackageResponseDto>> GetAllActiveAsync()
    {
        var packages = await _uow.ServicePackages
            .GetQueryable()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var result = new List<ServicePackageResponseDto>();
        foreach (var p in packages)
        {
            var currentSlots = await _uow.BuddySubscriptions.CountActiveByPackageIdAsync(p.Id);
            result.Add(MapToDto(p, currentSlots));
        }
        return result;
    }

    public async Task<ServicePackageResponseDto?> GetByIdAsync(Guid id)
    {
        var package = await _uow.ServicePackages.GetByIdAsync(id);
        if (package is null) return null;

        var currentSlots = await _uow.BuddySubscriptions.CountActiveByPackageIdAsync(id);
        return MapToDto(package, currentSlots);
    }

    // ─── Admin CRUD ──────────────────────────────────────────────────────────

    public async Task<IEnumerable<ServicePackageResponseDto>> GetAllAsync()
    {
        var packages = await _uow.ServicePackages
            .GetQueryable()
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var result = new List<ServicePackageResponseDto>();
        foreach (var p in packages)
        {
            var currentSlots = await _uow.BuddySubscriptions.CountActiveByPackageIdAsync(p.Id);
            result.Add(MapToDto(p, currentSlots));
        }
        return result;
    }

    public async Task<ServicePackageResponseDto> CreateAsync(ServicePackageCreateDto dto)
    {
        var package = new ServicePackage
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            PricePerMonth = dto.PricePerMonth,
            Currency = dto.Currency,
            CommissionRate = dto.CommissionRate,
            HasChatAccess = dto.HasChatAccess,
            HasSearchPriority = dto.HasSearchPriority,
            HasPrioritySupport = dto.HasPrioritySupport,
            HasProductFeedback = dto.HasProductFeedback,
            MaxSlots = dto.MaxSlots,
            SortOrder = dto.SortOrder,
            IsActive = true,
            Features = dto.Features,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.ServicePackages.AddAsync(package);
        await _uow.SaveChangesAsync();
        return MapToDto(package, 0);
    }

    public async Task<ServicePackageResponseDto?> UpdateAsync(Guid id, ServicePackageUpdateDto dto)
    {
        var package = await _uow.ServicePackages.GetByIdAsync(id);
        if (package is null) return null;

        if (dto.Name is not null) package.Name = dto.Name;
        if (dto.Description is not null) package.Description = dto.Description;
        if (dto.PricePerMonth.HasValue) package.PricePerMonth = dto.PricePerMonth.Value;
        if (dto.Currency is not null) package.Currency = dto.Currency;
        if (dto.CommissionRate.HasValue) package.CommissionRate = dto.CommissionRate.Value;
        if (dto.HasChatAccess.HasValue) package.HasChatAccess = dto.HasChatAccess.Value;
        if (dto.HasSearchPriority.HasValue) package.HasSearchPriority = dto.HasSearchPriority.Value;
        if (dto.HasPrioritySupport.HasValue) package.HasPrioritySupport = dto.HasPrioritySupport.Value;
        if (dto.HasProductFeedback.HasValue) package.HasProductFeedback = dto.HasProductFeedback.Value;
        if (dto.MaxSlots.HasValue) package.MaxSlots = dto.MaxSlots.Value;
        if (dto.SortOrder.HasValue) package.SortOrder = dto.SortOrder.Value;
        if (dto.IsActive.HasValue) package.IsActive = dto.IsActive.Value;
        if (dto.Features is not null) package.Features = dto.Features;
        package.UpdatedAt = DateTime.UtcNow;

        _uow.ServicePackages.Update(package);
        await _uow.SaveChangesAsync();

        var currentSlots = await _uow.BuddySubscriptions.CountActiveByPackageIdAsync(id);
        return MapToDto(package, currentSlots);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var package = await _uow.ServicePackages.GetByIdAsync(id);
        if (package is null) return false;

        package.IsActive = false;
        package.UpdatedAt = DateTime.UtcNow;
        _uow.ServicePackages.Update(package);
        await _uow.SaveChangesAsync();
        return true;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BUDDY SUBSCRIPTION — Upgrade (proration) / Downgrade (end-of-cycle)
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<object> SubscribeAsync(Guid userId, SubscribeRequestDto dto)
    {
        var usePayOs = string.Equals(dto.PreferredPaymentMethod, "PayOS", StringComparison.OrdinalIgnoreCase);

        // Chỉ kiểm tra PayPal config nếu user chọn PayPal
        if (!usePayOs)
            EnsurePayPalConfigured();

        var buddy = await _uow.Buddies.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Buddy profile not found for current user.");

        var newPackage = await _uow.ServicePackages.GetByIdAsync(dto.ServicePackageId)
            ?? throw new KeyNotFoundException("Service package not found.");
        if (!newPackage.IsActive)
            throw new InvalidOperationException("Service package is not active.");

        // PayOS chỉ hỗ trợ VND
        if (usePayOs && !string.Equals(newPackage.Currency, "VND", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PayOS chỉ hỗ trợ thanh toán VND. Vui lòng chọn PayPal cho gói USD.");

        // Check slot limit
        if (newPackage.MaxSlots.HasValue)
        {
            var currentCount = await _uow.BuddySubscriptions.CountActiveByPackageIdAsync(newPackage.Id);
            if (currentCount >= newPackage.MaxSlots.Value)
                throw new InvalidOperationException(
                    $"Gói \"{newPackage.Name}\" đã hết suất ({newPackage.MaxSlots.Value}/{newPackage.MaxSlots.Value}).");
        }

        // Cancel any stale pending-payment subscription
        var pendingSub = await _uow.BuddySubscriptions.GetPendingByBuddyIdAsync(buddy.Id);
        if (pendingSub is not null)
        {
            pendingSub.Status = SubscriptionStatus.Cancelled;
            pendingSub.UpdatedAt = DateTime.UtcNow;
            _uow.BuddySubscriptions.Update(pendingSub);
        }

        // Check if buddy has an active subscription
        var activeSub = await _uow.BuddySubscriptions.GetActiveByBuddyIdAsync(buddy.Id);

        if (activeSub is not null)
        {
            var currentPackage = activeSub.ServicePackage;

            // ─── Same package? Just clear any scheduled downgrade ────────────
            if (activeSub.ServicePackageId == dto.ServicePackageId)
            {
                if (activeSub.DowngradeToPackageId.HasValue)
                {
                    activeSub.DowngradeToPackageId = null;
                    activeSub.UpdatedAt = DateTime.UtcNow;
                    _uow.BuddySubscriptions.Update(activeSub);
                    await _uow.SaveChangesAsync();
                }
                if (activeSub.IsCancelledAtEndOfCycle)
                {
                    activeSub.IsCancelledAtEndOfCycle = false;
                    activeSub.UpdatedAt = DateTime.UtcNow;
                    _uow.BuddySubscriptions.Update(activeSub);
                    await _uow.SaveChangesAsync();
                }
                throw new InvalidOperationException("Bạn đang sử dụng gói này rồi.");
            }

            // ─── UPGRADE: new package is more expensive → proration ──────────
            if (newPackage.PricePerMonth > currentPackage.PricePerMonth)
            {
                return await HandleUpgradeAsync(buddy, activeSub, currentPackage, newPackage, usePayOs);
            }

            // ─── DOWNGRADE: new package is cheaper → schedule end-of-cycle ───
            return await HandleDowngradeAsync(activeSub, currentPackage, newPackage);
        }

        // ─── NEW SUBSCRIPTION: no active plan → full price ───────────────────
        return await CreateNewSubscriptionOrderAsync(buddy, newPackage, usePayOs);
    }

    // ─── Upgrade: proration + PayPal order for difference ────────────────────

    private async Task<object> HandleUpgradeAsync(
        Buddy buddy, BuddySubscription activeSub, ServicePackage oldPkg, ServicePackage newPkg, bool usePayOs = false)
    {
        var (creditFromOld, costForNew, proratedCharge) = SubscriptionProration.Calculate(
            oldPkg.PricePerMonth, newPkg.PricePerMonth, activeSub.StartDate, activeSub.EndDate, DateTime.UtcNow);

        // Also clear any scheduled downgrade
        if (activeSub.DowngradeToPackageId.HasValue)
        {
            activeSub.DowngradeToPackageId = null;
            activeSub.UpdatedAt = DateTime.UtcNow;
            _uow.BuddySubscriptions.Update(activeSub);
        }
        if (activeSub.IsCancelledAtEndOfCycle)
        {
            activeSub.IsCancelledAtEndOfCycle = false;
            activeSub.UpdatedAt = DateTime.UtcNow;
            _uow.BuddySubscriptions.Update(activeSub);
        }

        // Create pending upgrade subscription — keeps old sub's EndDate
        var upgradeSub = new BuddySubscription
        {
            Id = Guid.NewGuid(),
            BuddyId = buddy.Id,
            ServicePackageId = newPkg.Id,
            StartDate = DateTime.UtcNow,
            EndDate = activeSub.EndDate, // keep same billing cycle end
            Status = SubscriptionStatus.PendingPayment,
            PaymentMethod = usePayOs ? PaymentMethod.PayOS : PaymentMethod.Wallet,
            AmountPaid = proratedCharge,
            Currency = newPkg.Currency,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.BuddySubscriptions.AddAsync(upgradeSub);
        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "Upgrade proration: {OldPkg}→{NewPkg}, Credit: {Credit}, NewCost: {NewCost}, Charge: {Charge}",
            oldPkg.Name, newPkg.Name, creditFromOld, costForNew, proratedCharge);

        if (usePayOs)
        {
            return await CreatePayOsSubscriptionOrderAsync(upgradeSub, newPkg, proratedCharge,
                isUpgrade: true, creditFromOld: creditFromOld);
        }

        // PayPal flow
        var chargeAmount = proratedCharge > 0 ? proratedCharge : 1; // PayPal min = 1 VND
        var description = $"Upgrade Bonddy {oldPkg.Name} → {newPkg.Name} (Prorated)";

        var (orderId, approveUrl) = await CreatePayPalOrderAsync(
            upgradeSub.Id, chargeAmount, newPkg.Currency, description);

        upgradeSub.ExternalPaymentRef = orderId;
        upgradeSub.UpdatedAt = DateTime.UtcNow;
        _uow.BuddySubscriptions.Update(upgradeSub);
        await _uow.SaveChangesAsync();

        return new SubscriptionPayPalOrderResponseDto
        {
            SubscriptionId = upgradeSub.Id,
            ServicePackageId = newPkg.Id,
            PackageName = newPkg.Name,
            OrderId = orderId,
            ApproveUrl = approveUrl,
            Currency = newPkg.Currency,
            Amount = proratedCharge,
            IsUpgrade = true,
            CreditFromOldPlan = creditFromOld,
            ProratedCharge = proratedCharge
        };
    }

    // ─── Downgrade: schedule for end of current cycle ────────────────────────

    private async Task<DowngradeResponseDto> HandleDowngradeAsync(
        BuddySubscription activeSub, ServicePackage currentPkg, ServicePackage newPkg)
    {
        activeSub.DowngradeToPackageId = newPkg.Id;
        activeSub.IsCancelledAtEndOfCycle = false; // clear cancel if they're downgrading instead
        activeSub.UpdatedAt = DateTime.UtcNow;
        _uow.BuddySubscriptions.Update(activeSub);
        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "Downgrade scheduled: {CurrentPkg}→{NewPkg}, effective at {EndDate}",
            currentPkg.Name, newPkg.Name, activeSub.EndDate);

        return new DowngradeResponseDto
        {
            SubscriptionId = activeSub.Id,
            CurrentPackageName = currentPkg.Name,
            DowngradeToPackageName = newPkg.Name,
            EffectiveDate = activeSub.EndDate,
            Message = $"Gói {currentPkg.Name} vẫn active đến {activeSub.EndDate:dd/MM/yyyy}. " +
                      $"Sau đó sẽ tự động chuyển sang gói {newPkg.Name}."
        };
    }

    // ─── New subscription (no existing plan) ─────────────────────────────────

    private async Task<object> CreateNewSubscriptionOrderAsync(
        Buddy buddy, ServicePackage package, bool usePayOs = false)
    {
        var subscription = new BuddySubscription
        {
            Id = Guid.NewGuid(),
            BuddyId = buddy.Id,
            ServicePackageId = package.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Status = SubscriptionStatus.PendingPayment,
            PaymentMethod = usePayOs ? PaymentMethod.PayOS : PaymentMethod.Wallet,
            AmountPaid = package.PricePerMonth,
            Currency = package.Currency,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.BuddySubscriptions.AddAsync(subscription);
        await _uow.SaveChangesAsync();

        if (usePayOs)
        {
            return await CreatePayOsSubscriptionOrderAsync(subscription, package, package.PricePerMonth,
                isUpgrade: false, creditFromOld: 0);
        }

        // PayPal flow
        var description = $"Bonddy {package.Name} Package - 1 Month";
        var (orderId, approveUrl) = await CreatePayPalOrderAsync(
            subscription.Id, package.PricePerMonth, package.Currency, description);

        subscription.ExternalPaymentRef = orderId;
        subscription.UpdatedAt = DateTime.UtcNow;
        _uow.BuddySubscriptions.Update(subscription);
        await _uow.SaveChangesAsync();

        return new SubscriptionPayPalOrderResponseDto
        {
            SubscriptionId = subscription.Id,
            ServicePackageId = package.Id,
            PackageName = package.Name,
            OrderId = orderId,
            ApproveUrl = approveUrl,
            Currency = package.Currency,
            Amount = package.PricePerMonth,
            IsUpgrade = false,
            CreditFromOldPlan = 0,
            ProratedCharge = package.PricePerMonth
        };
    }

    // ─── PayOS: Create subscription payment link ─────────────────────────

    private async Task<SubscriptionPayOsOrderResponseDto> CreatePayOsSubscriptionOrderAsync(
        BuddySubscription subscription, ServicePackage package,
        decimal amount, bool isUpgrade, decimal creditFromOld)
    {
        // PayOS chỉ hỗ trợ VND — amount đã là VND từ package
        var amountVnd = (int)Math.Round(amount, 0);

        // PayOS minimum
        if (amountVnd < 2000) amountVnd = 2000;

        var orderCode = PayOsService.GenerateOrderCode();
        var description = $"BONDDY {package.Name[..Math.Min(package.Name.Length, 15)]}";

        var result = await _payOsService.CreatePaymentLinkAsync(
            orderCode, amountVnd, description);

        subscription.PayOsOrderCode = orderCode;
        subscription.ExternalPaymentRef = result.PaymentLinkId;
        subscription.UpdatedAt = DateTime.UtcNow;
        _uow.BuddySubscriptions.Update(subscription);
        await _uow.SaveChangesAsync();

        return new SubscriptionPayOsOrderResponseDto
        {
            SubscriptionId = subscription.Id,
            ServicePackageId = package.Id,
            PackageName = package.Name,
            OrderCode = orderCode,
            CheckoutUrl = result.CheckoutUrl,
            QrCode = result.QrCode,
            Currency = "VND",
            AmountVnd = amountVnd,
            IsUpgrade = isUpgrade,
            CreditFromOldPlan = creditFromOld,
            ProratedCharge = amount
        };
    }

    // ─── Capture Payment (for both new + upgrade) ────────────────────────────

    public async Task<BuddySubscriptionResponseDto> CaptureSubscriptionPaymentAsync(
        Guid userId, Guid subscriptionId, SubscriptionCaptureRequestDto dto)
    {
        EnsurePayPalConfigured();

        if (string.IsNullOrWhiteSpace(dto.OrderId))
            throw new InvalidOperationException("OrderId is required.");

        var normalizedOrderId = dto.OrderId.Trim();
        var ampIndex = normalizedOrderId.IndexOf('&');
        if (ampIndex > 0) normalizedOrderId = normalizedOrderId[..ampIndex];

        var subscription = await _uow.BuddySubscriptions.GetByIdAsync(subscriptionId)
            ?? throw new KeyNotFoundException("Subscription not found.");

        var buddy = await _uow.Buddies.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Buddy profile not found.");

        if (subscription.BuddyId != buddy.Id)
            throw new UnauthorizedAccessException("This subscription does not belong to you.");

        if (subscription.Status != SubscriptionStatus.PendingPayment)
            throw new InvalidOperationException($"Subscription is not pending payment. Current status: {subscription.Status}");

        var package = await _uow.ServicePackages.GetByIdAsync(subscription.ServicePackageId)
            ?? throw new KeyNotFoundException("Service package not found.");

        // Capture PayPal order
        var accessToken = await GetPayPalAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_payPalOptions.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var captureContent = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/v2/checkout/orders/{normalizedOrderId}/capture", captureContent);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal capture for subscription failed: {StatusCode} - {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Failed to capture PayPal payment for subscription.");
        }

        using var doc = JsonDocument.Parse(body);
        var status = doc.RootElement.TryGetProperty("status", out var statusEl)
            ? statusEl.GetString()
            : null;

        if (!string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"PayPal capture not completed. Status: {status ?? "UNKNOWN"}");

        // Cancel old active subscription (replaced by upgrade)
        var existingActive = await _uow.BuddySubscriptions.GetActiveByBuddyIdAsync(buddy.Id);
        if (existingActive is not null && existingActive.Id != subscription.Id)
        {
            existingActive.Status = SubscriptionStatus.Cancelled;
            existingActive.UpdatedAt = DateTime.UtcNow;
            _uow.BuddySubscriptions.Update(existingActive);
        }

        // Activate the new subscription
        subscription.Status = SubscriptionStatus.Active;
        subscription.PaidAt = DateTime.UtcNow;
        subscription.ExternalPaymentRef = normalizedOrderId;
        subscription.PaymentMethod = PaymentMethod.Wallet;
        subscription.UpdatedAt = DateTime.UtcNow;

        // For new subscriptions (not upgrade), set proper start/end dates
        if (existingActive is null)
        {
            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = DateTime.UtcNow.AddMonths(1);
        }
        // For upgrades, StartDate/EndDate were already set in HandleUpgradeAsync

        _uow.BuddySubscriptions.Update(subscription);
        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "Subscription {SubId} activated for Buddy {BuddyId} — Package: {Package}, Amount: {Amount} {Currency}",
            subscription.Id, buddy.Id, package.Name, subscription.AmountPaid, subscription.Currency);

        return MapSubscriptionToDto(subscription, package);
    }

    // ─── Get my subscription ─────────────────────────────────────────────────

    public async Task<BuddySubscriptionResponseDto?> GetMySubscriptionAsync(Guid userId)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(userId);
        if (buddy is null) return null;

        // Kiểm tra active subscription trước
        var sub = await _uow.BuddySubscriptions.GetActiveByBuddyIdAsync(buddy.Id);
        if (sub is not null)
            return MapSubscriptionToDto(sub, sub.ServicePackage);

        // Không có active → tìm pending PayOS subscription và tự verify
        var pendingSub = await _uow.BuddySubscriptions.GetPendingByBuddyIdAsync(buddy.Id);
        if (pendingSub is null || !pendingSub.PayOsOrderCode.HasValue)
            return null;

        // Gọi PayOS API kiểm tra trạng thái
        try
        {
            var paymentInfo = await _payOsService.GetPaymentInfoAsync(pendingSub.PayOsOrderCode.Value);
            if (paymentInfo is null || !string.Equals(paymentInfo.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "PayOS pending subscription {SubId}: status={Status} — not yet paid",
                    pendingSub.Id, paymentInfo?.Status ?? "N/A");
                return null;
            }

            // Đã PAID → activate
            pendingSub.Status = SubscriptionStatus.Active;
            pendingSub.PaidAt = DateTime.UtcNow;
            pendingSub.PaymentMethod = PaymentMethod.PayOS;
            pendingSub.StartDate = DateTime.UtcNow;
            pendingSub.EndDate = DateTime.UtcNow.AddMonths(1);
            pendingSub.UpdatedAt = DateTime.UtcNow;
            _uow.BuddySubscriptions.Update(pendingSub);
            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Auto-activated PayOS subscription {SubId} for Buddy {BuddyId} via GetMySubscription",
                pendingSub.Id, buddy.Id);

            return MapSubscriptionToDto(pendingSub, pendingSub.ServicePackage);
        }
        catch (Exception ex)
        {
            // Không để lỗi PayOS làm hỏng cả request
            _logger.LogWarning(ex,
                "Failed to auto-verify PayOS subscription {SubId} — returning null",
                pendingSub.Id);
            return null;
        }
    }

    // ─── Cancel: giữ đến hết kỳ ─────────────────────────────────────────────

    public async Task<BuddySubscriptionResponseDto?> CancelSubscriptionAsync(Guid userId)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(userId);
        if (buddy is null) return null;

        var sub = await _uow.BuddySubscriptions.GetActiveByBuddyIdAsync(buddy.Id);
        if (sub is null) return null;

        // Mark as cancelled at end of cycle — buddy keeps access until EndDate
        sub.IsCancelledAtEndOfCycle = true;
        sub.DowngradeToPackageId = null; // clear any scheduled downgrade
        sub.UpdatedAt = DateTime.UtcNow;
        _uow.BuddySubscriptions.Update(sub);
        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "Subscription {SubId} marked for cancellation at end of cycle ({EndDate})",
            sub.Id, sub.EndDate);

        return MapSubscriptionToDto(sub, sub.ServicePackage);
    }

    // ─── Verify PayOS subscription payment (client-side poll after redirect) ──

    public async Task<BuddySubscriptionResponseDto> VerifyPayOsSubscriptionAsync(Guid userId, Guid subscriptionId)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Buddy profile not found.");

        var subscription = await _uow.BuddySubscriptions.GetByIdAsync(subscriptionId)
            ?? throw new KeyNotFoundException("Subscription not found.");

        if (subscription.BuddyId != buddy.Id)
            throw new UnauthorizedAccessException("This subscription does not belong to you.");

        var package = await _uow.ServicePackages.GetByIdAsync(subscription.ServicePackageId)
            ?? throw new KeyNotFoundException("Service package not found.");

        // Nếu đã active rồi → trả về luôn
        if (subscription.Status == SubscriptionStatus.Active)
            return MapSubscriptionToDto(subscription, package);

        if (subscription.Status != SubscriptionStatus.PendingPayment)
            throw new InvalidOperationException($"Subscription is not pending payment. Status: {subscription.Status}");

        if (!subscription.PayOsOrderCode.HasValue)
            throw new InvalidOperationException("Subscription does not have a PayOS order code.");

        // Gọi PayOS API để kiểm tra trạng thái thanh toán
        var paymentInfo = await _payOsService.GetPaymentInfoAsync(subscription.PayOsOrderCode.Value)
            ?? throw new InvalidOperationException("Cannot retrieve payment info from PayOS.");

        _logger.LogInformation(
            "PayOS payment info for orderCode={OrderCode}: status={Status}, amountPaid={AmountPaid}",
            subscription.PayOsOrderCode.Value, paymentInfo.Status, paymentInfo.AmountPaid);

        if (!string.Equals(paymentInfo.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"PayOS payment has not been completed yet. Current status: {paymentInfo.Status}");

        // Thanh toán thành công → activate subscription
        var existingActive = await _uow.BuddySubscriptions.GetActiveByBuddyIdAsync(buddy.Id);
        if (existingActive is not null && existingActive.Id != subscription.Id)
        {
            existingActive.Status = SubscriptionStatus.Cancelled;
            existingActive.UpdatedAt = DateTime.UtcNow;
            _uow.BuddySubscriptions.Update(existingActive);
        }

        subscription.Status = SubscriptionStatus.Active;
        subscription.PaidAt = DateTime.UtcNow;
        subscription.PaymentMethod = PaymentMethod.PayOS;
        subscription.UpdatedAt = DateTime.UtcNow;

        if (existingActive is null)
        {
            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = DateTime.UtcNow.AddMonths(1);
        }

        _uow.BuddySubscriptions.Update(subscription);
        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "Subscription {SubId} activated via PayOS verify for Buddy {BuddyId} — Package: {Package}",
            subscription.Id, buddy.Id, package.Name);

        return MapSubscriptionToDto(subscription, package);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PayPal Helpers
    // ═══════════════════════════════════════════════════════════════════════════


    private void EnsurePayPalConfigured()
    {
        if (string.IsNullOrWhiteSpace(_payPalOptions.BaseUrl) ||
            string.IsNullOrWhiteSpace(_payPalOptions.ClientId) ||
            string.IsNullOrWhiteSpace(_payPalOptions.Secret))
        {
            throw new InvalidOperationException("PayPal is not configured. Missing ClientId/Secret/BaseUrl.");
        }
    }

    /// <summary>Creates a PayPal order and returns (orderId, approveUrl).</summary>
    private async Task<(string orderId, string? approveUrl)> CreatePayPalOrderAsync(
        Guid referenceId, decimal amount, string currency, string description)
    {
        var accessToken = await GetPayPalAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_payPalOptions.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var orderRequest = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = referenceId.ToString(),
                    custom_id = $"sub_{referenceId}",
                    description,
                    amount = new
                    {
                        currency_code = currency,
                        value = amount.ToString("0.00", CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                brand_name = "Bonddy",
                return_url = _payPalOptions.ReturnUrl,
                cancel_url = _payPalOptions.CancelUrl
            }
        };

        var response = await client.PostAsync(
            "/v2/checkout/orders",
            new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json"));

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal create order failed: {StatusCode} - {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Failed to create PayPal order.");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var orderId = root.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("PayPal response missing order id.");

        string? approveUrl = null;
        if (root.TryGetProperty("links", out var linksEl) && linksEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var link in linksEl.EnumerateArray())
            {
                if (string.Equals(link.GetProperty("rel").GetString(), "approve", StringComparison.OrdinalIgnoreCase))
                {
                    approveUrl = link.GetProperty("href").GetString();
                    break;
                }
            }
        }

        return (orderId, approveUrl);
    }

    private async Task<string> GetPayPalAccessTokenAsync()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_payPalOptions.BaseUrl);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_payPalOptions.ClientId}:{_payPalOptions.Secret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        var response = await client.PostAsync("/v1/oauth2/token", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal token request failed: {StatusCode} - {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Failed to get PayPal access token.");
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("PayPal token response missing access_token.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Mappers
    // ═══════════════════════════════════════════════════════════════════════════

    private static ServicePackageResponseDto MapToDto(ServicePackage p, int currentSlots) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        PricePerMonth = p.PricePerMonth,
        Currency = p.Currency,
        CommissionRate = p.CommissionRate,
        HasChatAccess = p.HasChatAccess,
        HasSearchPriority = p.HasSearchPriority,
        HasPrioritySupport = p.HasPrioritySupport,
        HasProductFeedback = p.HasProductFeedback,
        MaxSlots = p.MaxSlots,
        CurrentSlots = currentSlots,
        SortOrder = p.SortOrder,
        IsActive = p.IsActive,
        Features = p.Features,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };

    private static BuddySubscriptionResponseDto MapSubscriptionToDto(BuddySubscription sub, ServicePackage pkg) => new()
    {
        Id = sub.Id,
        BuddyId = sub.BuddyId,
        ServicePackageId = sub.ServicePackageId,
        PackageName = pkg.Name,
        CommissionRate = pkg.CommissionRate,
        StartDate = sub.StartDate,
        EndDate = sub.EndDate,
        Status = sub.Status.ToString(),
        PaymentMethod = sub.PaymentMethod.ToString(),
        AmountPaid = sub.AmountPaid,
        Currency = sub.Currency,
        ExternalPaymentRef = sub.ExternalPaymentRef,
        PaidAt = sub.PaidAt,
        DowngradeToPackageId = sub.DowngradeToPackageId,
        DowngradeToPackageName = sub.DowngradeToPackage?.Name,
        IsCancelledAtEndOfCycle = sub.IsCancelledAtEndOfCycle,
        CreatedAt = sub.CreatedAt,
        UpdatedAt = sub.UpdatedAt
    };
}
