namespace FormCMS.Subscriptions.Models;

public record Customer(string Email, string? PaymentMethodId, string Name, string Id);

