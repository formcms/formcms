using FormCMS.Cms.Services;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Subscriptions.Models;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Subscriptions.Services;

public class BillingService(
    IIdentityService  identityService,
    IRelationDbDao dao,
    KateQueryExecutor executor
    ):IBillingService
{
    public Task UpsertBill(Billing billing, CancellationToken ct)
        => dao.UpdateOnConflict(Billings.TableName, billing.ToUpsertRecord(), [nameof(Billing.UserId).Camelize()], ct);

    public async Task<Billing?> GetSubBilling(CancellationToken ct)
    {
        var user = identityService.GetUserAccess() ?? throw new ResultException("User is not authorized");
        var query = Billings.ByUserId(user.Id);
        var ret = await executor.Single(query, ct);
        return ret?.ToObject<Billing>().Ok();
    }
}