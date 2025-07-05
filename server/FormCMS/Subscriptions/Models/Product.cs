namespace FormCMS.Subscriptions.Models;

public record Product(
    string ExternalId,
    string Name,
    long? Amount,
    string? Currency,
    string? Interval,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default
);