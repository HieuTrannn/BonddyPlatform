using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.ServicePackageDtos;
using BonddyPlatform.Services.Helpers;
using BonddyPlatform.Services.Implements;
using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;
using BonddyPlatform.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace BonddyPlatform.Tests;

public class SubscriptionProrationTests
{
    private static readonly DateTime Start = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = Start.AddDays(30);

    [Fact]
    public void Halfway_through_cycle_charges_half_the_price_difference()
    {
        var r = SubscriptionProration.Calculate(100_000, 300_000, Start, End, now: Start.AddDays(15));

        Assert.Equal(50_000, r.CreditFromOld);
        Assert.Equal(150_000, r.CostForNew);
        Assert.Equal(100_000, r.Charge);
    }

    [Fact]
    public void Upgrade_on_first_day_charges_full_difference()
    {
        var r = SubscriptionProration.Calculate(100_000, 300_000, Start, End, now: Start);

        Assert.Equal(200_000, r.Charge);
    }

    [Fact]
    public void Upgrade_after_cycle_end_costs_nothing()
    {
        var r = SubscriptionProration.Calculate(100_000, 300_000, Start, End, now: End.AddDays(3));

        Assert.Equal(0, r.CreditFromOld);
        Assert.Equal(0, r.Charge);
    }

    [Fact]
    public void Clock_before_cycle_start_never_charges_more_than_the_full_difference()
    {
        var r = SubscriptionProration.Calculate(100_000, 300_000, Start, End, now: Start.AddDays(-10));

        Assert.Equal(200_000, r.Charge);
    }

    [Fact]
    public void Charge_is_never_negative()
    {
        // Defensive: a "cheaper" target must not produce a refund through this path.
        var r = SubscriptionProration.Calculate(300_000, 100_000, Start, End, now: Start.AddDays(10));

        Assert.Equal(0, r.Charge);
    }

    [Fact]
    public void Zero_length_cycle_does_not_divide_by_zero()
    {
        var r = SubscriptionProration.Calculate(100_000, 300_000, Start, Start, now: Start);

        Assert.Equal(0, r.Charge);
    }

    [Fact]
    public void Amounts_are_rounded_to_whole_VND()
    {
        var r = SubscriptionProration.Calculate(99_999, 199_999, Start, End, now: Start.AddDays(10));

        Assert.Equal(decimal.Round(r.Charge), r.Charge);
        Assert.Equal(decimal.Round(r.CreditFromOld), r.CreditFromOld);
    }
}

public class SubscriptionUpgradeDowngradeTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IBuddySubscriptionRepository _subs = Substitute.For<IBuddySubscriptionRepository>();
    private readonly IPayOsService _payOs = Substitute.For<IPayOsService>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Buddy _buddy;
    private readonly ServicePackage _basic = Package("Basic", 100_000);
    private readonly ServicePackage _gold = Package("Gold", 300_000);
    private readonly BuddySubscription _active;

    public SubscriptionUpgradeDowngradeTests()
    {
        _buddy = new Buddy { Id = Guid.NewGuid(), UserId = _userId };

        _active = new BuddySubscription
        {
            Id = Guid.NewGuid(),
            BuddyId = _buddy.Id,
            ServicePackageId = _gold.Id,
            ServicePackage = _gold,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-15),
            EndDate = DateTime.UtcNow.AddDays(15)
        };

        var buddies = Substitute.For<IBuddyRepository>();
        buddies.GetByUserIdAsync(_userId).Returns(_buddy);

        var packages = Substitute.For<IServicePackageRepository>();
        packages.GetByIdAsync(_basic.Id).Returns(_basic);
        packages.GetByIdAsync(_gold.Id).Returns(_gold);

        _uow.Buddies.Returns(buddies);
        _uow.ServicePackages.Returns(packages);
        _uow.BuddySubscriptions.Returns(_subs);
        _subs.GetActiveByBuddyIdAsync(_buddy.Id).Returns(_active);

        _payOs.CreatePaymentLinkAsync(default, default, default!)
            .ReturnsForAnyArgs(ci => new PayOsCreateLinkResult
            {
                CheckoutUrl = "https://pay.test/checkout",
                PaymentLinkId = "link-1",
                OrderCode = ci.ArgAt<long>(0),
                Amount = ci.ArgAt<int>(1)
            });
    }

    private static ServicePackage Package(string name, decimal price) =>
        new() { Id = Guid.NewGuid(), Name = name, PricePerMonth = price, Currency = "VND", IsActive = true };

    private ServicePackageService CreateService() => new(
        _uow,
        MsOptions.Create(new PayPalOptions()),
        NullLogger<ServicePackageService>.Instance,
        _payOs,
        new StubHttpClientFactory(new StubHttpMessageHandler()));

    private Task<object> SubscribeAsync(ServicePackage target) =>
        CreateService().SubscribeAsync(_userId, new SubscribeRequestDto
        {
            ServicePackageId = target.Id,
            PreferredPaymentMethod = "PayOS"
        });

    [Fact]
    public async Task Downgrade_is_scheduled_for_end_of_cycle_without_charging()
    {
        var result = await SubscribeAsync(_basic);

        var downgrade = Assert.IsType<DowngradeResponseDto>(result);
        Assert.Equal(_active.EndDate, downgrade.EffectiveDate);
        Assert.Equal(_basic.Id, _active.DowngradeToPackageId);
        Assert.Equal(SubscriptionStatus.Active, _active.Status);   // still on Gold until the cycle ends
        Assert.Equal(_gold.Id, _active.ServicePackageId);
        await _subs.DidNotReceiveWithAnyArgs().AddAsync(default!);
        await _payOs.DidNotReceiveWithAnyArgs().CreatePaymentLinkAsync(default, default, default!);
    }

    [Fact]
    public async Task Downgrade_cancels_a_pending_end_of_cycle_cancellation()
    {
        _active.IsCancelledAtEndOfCycle = true;

        await SubscribeAsync(_basic);

        Assert.False(_active.IsCancelledAtEndOfCycle);
    }

    [Fact]
    public async Task Upgrade_charges_prorated_difference_and_keeps_billing_cycle()
    {
        _active.ServicePackageId = _basic.Id;
        _active.ServicePackage = _basic;
        BuddySubscription? created = null;
        await _subs.AddAsync(Arg.Do<BuddySubscription>(s => created = s));

        var result = await SubscribeAsync(_gold);

        var order = Assert.IsType<SubscriptionPayOsOrderResponseDto>(result);
        Assert.True(order.IsUpgrade);
        Assert.InRange(order.AmountVnd, 99_000, 101_000);  // ~half of (300k - 100k)
        Assert.InRange(order.CreditFromOldPlan, 49_000m, 51_000m);

        Assert.NotNull(created);
        Assert.Equal(_gold.Id, created!.ServicePackageId);
        Assert.Equal(SubscriptionStatus.PendingPayment, created.Status);
        Assert.Equal(_active.EndDate, created.EndDate);        // same cycle end, no free extra days
        Assert.Equal(SubscriptionStatus.Active, _active.Status); // old plan stays until payment succeeds

        await _payOs.Received(1).CreatePaymentLinkAsync(Arg.Any<long>(), order.AmountVnd, Arg.Any<string>());
    }

    [Fact]
    public async Task Upgrade_clears_a_previously_scheduled_downgrade()
    {
        _active.ServicePackageId = _basic.Id;
        _active.ServicePackage = _basic;
        _active.DowngradeToPackageId = Guid.NewGuid();

        await SubscribeAsync(_gold);

        Assert.Null(_active.DowngradeToPackageId);
    }

    [Fact]
    public async Task Tiny_prorated_upgrade_is_raised_to_PayOS_minimum()
    {
        _active.ServicePackageId = _basic.Id;
        _active.ServicePackage = _basic;
        _active.EndDate = DateTime.UtcNow.AddMinutes(1); // almost nothing left in the cycle

        var order = Assert.IsType<SubscriptionPayOsOrderResponseDto>(await SubscribeAsync(_gold));

        Assert.Equal(2000, order.AmountVnd);
    }

    [Fact]
    public async Task Resubscribing_to_current_plan_is_rejected_but_undoes_a_scheduled_downgrade()
    {
        _active.DowngradeToPackageId = _basic.Id;

        await Assert.ThrowsAsync<InvalidOperationException>(() => SubscribeAsync(_gold));

        Assert.Null(_active.DowngradeToPackageId);
        await _payOs.DidNotReceiveWithAnyArgs().CreatePaymentLinkAsync(default, default, default!);
    }

    [Fact]
    public async Task Stale_pending_subscription_is_cancelled_before_a_new_order()
    {
        var stale = new BuddySubscription { Id = Guid.NewGuid(), BuddyId = _buddy.Id, Status = SubscriptionStatus.PendingPayment };
        _subs.GetPendingByBuddyIdAsync(_buddy.Id).Returns(stale);

        await SubscribeAsync(_basic);

        Assert.Equal(SubscriptionStatus.Cancelled, stale.Status);
    }

    [Fact]
    public async Task Full_package_is_rejected()
    {
        _basic.MaxSlots = 10;
        _subs.CountActiveByPackageIdAsync(_basic.Id).Returns(10);

        await Assert.ThrowsAsync<InvalidOperationException>(() => SubscribeAsync(_basic));
        Assert.Null(_active.DowngradeToPackageId);
    }
}
