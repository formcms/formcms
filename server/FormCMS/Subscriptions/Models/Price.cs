namespace FormCMS.Subscriptions.Models;

public record Price(
    string Id,
    string Name,
    string Currency,
    decimal Amount,
    string Description,
    string Interval,
    DateTime? NextBillingDate = null
);

public static class Prices
{
    public static Price GetNextBillingDate(this Price price, DateTime anchor)
    {
        var now = DateTime.UtcNow;

        DateTime nextBillingDate;

        switch (price.Interval.ToLower())
        {
            case "month":
                var monthsSinceAnchor = (int)((now - anchor).TotalDays / 30.0);
                nextBillingDate = anchor.AddMonths(monthsSinceAnchor + 1);
                break;

            case "year":
                var yearsSinceAnchor = (int)((now - anchor).TotalDays / 365.0);
                nextBillingDate = anchor.AddYears(yearsSinceAnchor + 1);
                break;

            default:
                throw new ArgumentException($"Unsupported billing interval: {price.Interval}", nameof(price.Interval));
        } 
        return price with{NextBillingDate = nextBillingDate};
    }
}
    