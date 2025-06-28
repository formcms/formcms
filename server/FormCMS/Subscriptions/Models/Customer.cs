namespace FormCMS.Subscriptions.Models;

public record  StripeCustomer(string Email,string? PaymentMethodId,string Name,string Id):ICustomer;

