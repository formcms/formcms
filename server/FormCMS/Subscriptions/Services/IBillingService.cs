using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Services;

public interface IBillingService
{
    Task UpsertBill(Billing billing, CancellationToken ct);
    Task<Billing?> GetSubBilling(CancellationToken ct);
}