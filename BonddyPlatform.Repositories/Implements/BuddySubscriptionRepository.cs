using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class BuddySubscriptionRepository : GenericRepository<BuddySubscription>, IBuddySubscriptionRepository
{
    public BuddySubscriptionRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<BuddySubscription?> GetActiveByBuddyIdAsync(Guid buddyId)
        => await _context.BuddySubscriptions
            .Include(s => s.ServicePackage)
            .Include(s => s.Buddy)
            .FirstOrDefaultAsync(s => s.BuddyId == buddyId && s.Status == SubscriptionStatus.Active);

    public async Task<int> CountActiveByPackageIdAsync(Guid packageId)
        => await _context.BuddySubscriptions
            .CountAsync(s => s.ServicePackageId == packageId && s.Status == SubscriptionStatus.Active);

    public async Task<BuddySubscription?> GetByExternalPaymentRefAsync(string externalRef)
        => await _context.BuddySubscriptions
            .Include(s => s.ServicePackage)
            .Include(s => s.Buddy)
            .FirstOrDefaultAsync(s => s.ExternalPaymentRef == externalRef);

    public async Task<BuddySubscription?> GetPendingByBuddyIdAsync(Guid buddyId)
        => await _context.BuddySubscriptions
            .Include(s => s.ServicePackage)
            .Include(s => s.Buddy)
            .Where(s => s.BuddyId == buddyId && s.Status == SubscriptionStatus.PendingPayment)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
}
