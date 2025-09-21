namespace FormCMS.Subscriptions.Models;

public record Product(
    string Name,
    long? Amount,
    string? Currency,
    string? Interval,
    string? id = default,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default,
    string? DefaultPriceId = default,
    string? stripeId = default
);
