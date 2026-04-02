using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IBuddySubscriptionRepository : IGenericRepository<BuddySubscription>
{
    /// <summary>Get the currently active subscription for a buddy (with ServicePackage included).</summary>
    Task<BuddySubscription?> GetActiveByBuddyIdAsync(Guid buddyId);

    /// <summary>Count how many active subscriptions exist for a given package (used for slot limits).</summary>
    Task<int> CountActiveByPackageIdAsync(Guid packageId);

    /// <summary>Find a pending-payment subscription by its external payment reference.</summary>
    Task<BuddySubscription?> GetByExternalPaymentRefAsync(string externalRef);

    /// <summary>Find the latest pending-payment subscription for a buddy.</summary>
    Task<BuddySubscription?> GetPendingByBuddyIdAsync(Guid buddyId);
}
