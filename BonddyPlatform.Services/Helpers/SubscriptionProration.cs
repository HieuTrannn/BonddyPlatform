namespace BonddyPlatform.Services.Helpers;

/// <summary>Result of prorating a mid-cycle plan upgrade.</summary>
public readonly record struct ProrationResult(decimal CreditFromOld, decimal CostForNew, decimal Charge);

/// <summary>
/// Upgrade pricing: the buddy pays only the price difference for the days left in the current billing cycle.
/// Amounts are rounded to whole units (VND has no minor unit) and the charge is never negative.
/// </summary>
public static class SubscriptionProration
{
    public static ProrationResult Calculate(
        decimal oldPricePerMonth, decimal newPricePerMonth,
        DateTime cycleStart, DateTime cycleEnd, DateTime now)
    {
        var totalDays = (cycleEnd - cycleStart).TotalDays;
        var remainingDays = Math.Max(0, (cycleEnd - now).TotalDays);
        var ratio = totalDays > 0 ? Math.Min(1, remainingDays / totalDays) : 0;

        var creditFromOld = Math.Round(oldPricePerMonth * (decimal)ratio, 0);
        var costForNew = Math.Round(newPricePerMonth * (decimal)ratio, 0);
        var charge = Math.Max(0, costForNew - creditFromOld);

        return new ProrationResult(creditFromOld, costForNew, charge);
    }
}
