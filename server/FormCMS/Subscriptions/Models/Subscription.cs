namespace FormCMS.Subscriptions.Models
{
    public record Subscription(
        string ExternalId,
        string CustomerId,

        string? ProductId,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string Status,
        string? PriceId = null
    );
}
